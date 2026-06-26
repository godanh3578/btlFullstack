using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.DTOs.Suppliers;
using OrderApi.Models;

namespace OrderApi.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly OrderDbContext _context;

        public SupplierService(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SupplierDto>> GetAllAsync(string? search)
        {
            var query = _context.Suppliers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(s =>
                    s.SupplierName.Contains(term) ||
                    s.SupplierCode.Contains(term) ||
                    s.ContactPerson.Contains(term) ||
                    s.Phone.Contains(term) ||
                    s.TaxCode.Contains(term));
            }

            var suppliers = await query.OrderBy(s => s.SupplierName).ToListAsync();
            return suppliers.Select(MapToDto);
        }

        public async Task<SupplierDto?> GetByIdAsync(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return null;
            return MapToDto(supplier);
        }

        public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
        {
            var exists = await _context.Suppliers.AnyAsync(s => s.SupplierCode == dto.SupplierCode);
            if (exists)
                throw new InvalidOperationException($"Supplier with code {dto.SupplierCode} already exists.");

            var supplier = new Supplier
            {
                SupplierCode = dto.SupplierCode,
                SupplierName = dto.SupplierName,
                ContactPerson = dto.ContactPerson,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                TaxCode = dto.TaxCode,
                Note = dto.Note,
                Status = SupplierStatus.Active
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return MapToDto(supplier);
        }

        public async Task<SupplierDto?> UpdateAsync(int id, UpdateSupplierDto dto)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return null;

            supplier.SupplierName = dto.SupplierName;
            supplier.ContactPerson = dto.ContactPerson;
            supplier.Phone = dto.Phone;
            supplier.Email = dto.Email;
            supplier.Address = dto.Address;
            supplier.TaxCode = dto.TaxCode;
            supplier.Note = dto.Note;

            if (Enum.TryParse<SupplierStatus>(dto.Status, true, out var status))
                supplier.Status = status;

            supplier.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapToDto(supplier);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return false;

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            return true;
        }

        private static SupplierDto MapToDto(Supplier supplier) => new()
        {
            SupplierId = supplier.SupplierId,
            SupplierCode = supplier.SupplierCode,
            SupplierName = supplier.SupplierName,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            TaxCode = supplier.TaxCode,
            Note = supplier.Note,
            Status = supplier.Status.ToString(),
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        };
    }
}
