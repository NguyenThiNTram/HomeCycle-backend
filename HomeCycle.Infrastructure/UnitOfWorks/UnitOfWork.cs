using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Repositories.Generics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.UnitOfWorks
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HomeCycleDbContext _db;
        private readonly Hashtable _repositories;
        private IDbContextTransaction? _currentTransaction;
        private int _transactionCount = 0;
        private readonly ILogger<UnitOfWork> _logger;

        private readonly List<Func<Task>> _afterCommitActions = [];
        private readonly List<Func<Task>> _afterRollbackActions = [];

        public UnitOfWork(HomeCycleDbContext db, ILogger<UnitOfWork> logger)
        {
            _db = db;
            _repositories = new Hashtable();
            _logger = logger;
        }

        // Tự động khởi tạo và cache Repository
        public IGenericRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(GenericRepository<>);
                var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _db)!;
                _repositories.Add(type, repositoryInstance);
            }

            return (IGenericRepository<T>)_repositories[type]!;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            //return _db.SaveChangesAsync(cancellationToken);
            var hasExplicitTransaction = _currentTransaction is not null;

            try
            {
                var affectedRows = await _db.SaveChangesAsync(cancellationToken);

                // SaveChanges tự tạo implicit transaction khi bên ngoài
                // không có explicit transaction.
                if (!hasExplicitTransaction)
                {
                    await ExecuteCallbacksSafelyAsync( _afterCommitActions, "after commit");

                    ClearTransactionCallbacks();
                }

                return affectedRows;
            }
            catch
            {
                // implicit transaction khi SaveChanges thất bại.
                if (!hasExplicitTransaction)
                {
                    await ExecuteCallbacksSafelyAsync(
                        _afterRollbackActions,
                        "after rollback");

                    ClearTransactionCallbacks();
                }

                // Nếu có explicit transaction, giữ callback lại -> Caller sẽ gọi RollbackTransactionAsync
                throw;
            }
        }

        // Quản lý Transaction thủ công khi gọi nhiều API/Service phức tạp
        // Dùng khi lưu dữ liệu làm nhiều đợt
        // Chỉ khi nào bước x thành công và dùng lệnh _currentTransaction.Commit(), dữ liệu mới thực sự được lưu vào Database
        //public async Task BeginTransactionAsync() => _currentTransaction = await _db.Database.BeginTransactionAsync();
        //public async Task CommitTransactionAsync() => await _currentTransaction!.CommitAsync();
        //public async Task RollbackTransactionAsync() => await _currentTransaction!.RollbackAsync();

        //public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        //{
        //    if (_transactionCount == 0)
        //        _currentTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        //    _transactionCount++;
        //}

        //public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        //{
        //    _transactionCount--;
        //    if (_transactionCount == 0 && _currentTransaction is not null)
        //    {
        //        await _currentTransaction.CommitAsync(cancellationToken);
        //        await _currentTransaction.DisposeAsync();
        //        _currentTransaction = null;
        //    }
        //}

        //public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        //{
        //    _transactionCount = 0;
        //    if (_currentTransaction is not null)
        //    {
        //        await _currentTransaction.RollbackAsync(cancellationToken);
        //        await _currentTransaction.DisposeAsync();
        //        _currentTransaction = null;
        //    }
        //}

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction is not null)
            {
                throw new InvalidOperationException(
                    "UnitOfWork đang có một transaction chưa được hoàn tất.");
            }

            if (_db.Database.CurrentTransaction is not null)
            {
                throw new InvalidOperationException(
                    "DbContext đang tham gia một transaction khác.");
            }

            _currentTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction is null)
            {
                throw new InvalidOperationException(
                    "Không có transaction đang hoạt động để commit.");
            }

            try
            {
                await _currentTransaction.CommitAsync(cancellationToken);
                // Database đã commit success
                await ExecuteCallbacksSafelyAsync( _afterCommitActions, "after commit");
                ClearTransactionCallbacks();
            }
            catch (Exception commitException)
            {
                _logger.LogError(
                    commitException,
                    "Không thể commit database transaction.");

                var rollbackSucceeded = false;

                try
                {
                    // Không dùng request token vì token có thể đã bị hủy.
                    await _currentTransaction.RollbackAsync(
                        CancellationToken.None);

                    rollbackSucceeded = true;
                }
                catch (Exception rollbackException)
                {
                    // Có thể commit đã được database xử lý nhưng client
                    // không nhận được phản hồi. Không được xóa file mới
                    // khi trạng thái transaction chưa chắc chắn.
                    _logger.LogCritical(
                        rollbackException,
                        "Commit thất bại và không thể xác nhận rollback transaction.");
                }

                if (rollbackSucceeded)
                {
                    await ExecuteCallbacksSafelyAsync(
                        _afterRollbackActions,
                        "after rollback");
                }

                ClearTransactionCallbacks();
                throw;
            }
            finally
            {
                await DisposeCurrentTransactionAsync();
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction is null)
                return;

            var rollbackSucceeded = false;

            try
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
                rollbackSucceeded = true;

                await ExecuteCallbacksSafelyAsync( _afterRollbackActions, "after rollback");
            }
            finally
            {
                if (!rollbackSucceeded)
                {
                    _logger.LogCritical(
                        "Không thể xác nhận database transaction đã rollback. " +
                        "Các callback rollback không được thực thi.");
                }

                ClearTransactionCallbacks();
                await DisposeCurrentTransactionAsync();
            }
        }

        public void RegisterAfterCommit(Func<Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            _afterCommitActions.Add(action);
        }

        public void RegisterAfterRollback(Func<Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            _afterRollbackActions.Add(action);
        }

        private async Task DisposeCurrentTransactionAsync()
        {
            if (_currentTransaction is null)
                return;

            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }

        public void Dispose()
        {
            ClearTransactionCallbacks();
            _currentTransaction?.Dispose();

            _db.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task ExecuteCallbacksSafelyAsync(IReadOnlyList<Func<Task>> callbacks, string callbackType)
        {
            foreach (var callback in callbacks)
            {
                try
                {
                    await callback();
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Không thể thực thi callback {CallbackType} của UnitOfWork.",
                        callbackType);
                }
            }
        }

        private void ClearTransactionCallbacks()
        {
            _afterCommitActions.Clear();
            _afterRollbackActions.Clear();
        }
    }
}

//Hashtable: Unit of Work sẽ vào Hashtable để kiểm tra -> Nếu đã có, nó lấy luôn trong Hashtable trả về(tiết kiệm bộ nhớ) -> Nếu chưa có, nó dùng Reflection để khởi tạo mới Repo, ném vào Hashtable để lưu lại, rồi trả về -->  bộ nhớ đệm (Cache) chứa các Repository
