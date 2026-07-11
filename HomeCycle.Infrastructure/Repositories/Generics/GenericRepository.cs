using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Generics
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly HomeCycleDbContext _db;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(HomeCycleDbContext db)
        {
            _db = db;
            _dbSet = _db.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        // Tối ưu: Dùng AsNoTracking cho các truy vấn Read-Only để tăng tốc độ
        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
        }

        public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
        }

        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }
    }
}
//CancellationToken: hủy câu lệnh, giải phóng RAM và CPU cho Database

//"= default" cho các hàm mang tính chất phục vụ chung, Các hàm xử lý chuỗi, đọc file cấu hình, hoặc gọi API nội bộ dùng chung ở nhiều nơi.  Các tác vụ ngầm định kỳ mà nếu có bị ngắt giữa chừng hay chạy cố thêm một chút cũng không gây ảnh hưởng lớn đến logic hệ thống
