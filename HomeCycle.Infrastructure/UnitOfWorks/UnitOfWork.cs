using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Repositories.Generics;
using Microsoft.EntityFrameworkCore.Storage;
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

        public UnitOfWork(HomeCycleDbContext db)
        {
            _db = db;
            _repositories = new Hashtable();
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

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _db.SaveChangesAsync(cancellationToken);
        }

        // Quản lý Transaction thủ công khi gọi nhiều API/Service phức tạp
        // Dùng khi lưu dữ liệu làm nhiều đợt
        // Chỉ khi nào bước x thành công và dùng lệnh _currentTransaction.Commit(), dữ liệu mới thực sự được lưu vào Database
        public async Task BeginTransactionAsync() => _currentTransaction = await _db.Database.BeginTransactionAsync();
        public async Task CommitTransactionAsync() => await _currentTransaction!.CommitAsync();
        public async Task RollbackTransactionAsync() => await _currentTransaction!.RollbackAsync();

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transactionCount == 0)
                _currentTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            _transactionCount++;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transactionCount--;
            if (_transactionCount == 0 && _currentTransaction is not null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transactionCount = 0;
            if (_currentTransaction is not null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public void Dispose()
        {
            _db.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

//Hashtable: Unit of Work sẽ vào Hashtable để kiểm tra -> Nếu đã có, nó lấy luôn trong Hashtable trả về(tiết kiệm bộ nhớ) -> Nếu chưa có, nó dùng Reflection để khởi tạo mới Repo, ném vào Hashtable để lưu lại, rồi trả về -->  bộ nhớ đệm (Cache) chứa các Repository
