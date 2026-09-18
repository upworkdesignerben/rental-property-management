using Microsoft.EntityFrameworkCore;
using PropertyRental.Web.Data;
using PropertyRental.Web.Models;
using PropertyRental.Web.Services.Interfaces;
using PropertyRental.Web.ViewModels.Properties;

namespace PropertyRental.Web.Services;

public class PropertyService(ApplicationDbContext dbContext) : IPropertyService
{
    public async Task<PropertyListViewModel> GetListAsync(CancellationToken cancellationToken = default)
    {
        var properties = await dbContext.Properties
            .AsNoTracking()
            .OrderBy(property => property.Name)
            .Select(property => new PropertyListItemViewModel
            {
                Id = property.Id,
                Name = property.Name,
                Address = property.Address,
                Description = property.Description,
                UnitCount = property.Units.Count
            })
            .ToListAsync(cancellationToken);

        return new PropertyListViewModel { Properties = properties };
    }

    public Task<PropertyFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Properties
            .AsNoTracking()
            .Where(property => property.Id == id)
            .Select(property => new PropertyFormViewModel
            {
                Id = property.Id,
                Name = property.Name,
                Address = property.Address,
                Description = property.Description
            })
            .SingleOrDefaultAsync(cancellationToken);

    public async Task CreateAsync(PropertyFormViewModel model, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        dbContext.Properties.Add(new Property
        {
            Name = model.Name.Trim(),
            Address = model.Address.Trim(),
            Description = model.Description.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, PropertyFormViewModel model, CancellationToken cancellationToken = default)
    {
        var property = await dbContext.Properties.SingleOrDefaultAsync(property => property.Id == id, cancellationToken);
        if (property is null)
        {
            return false;
        }

        property.Name = model.Name.Trim();
        property.Address = model.Address.Trim();
        property.Description = model.Description.Trim();
        property.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PropertyDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var property = await dbContext.Properties
            .Include(property => property.Units)
            .SingleOrDefaultAsync(property => property.Id == id, cancellationToken);
        if (property is null)
        {
            return new PropertyDeleteResult(false, false);
        }

        if (property.Units.Count != 0)
        {
            return new PropertyDeleteResult(true, true);
        }

        dbContext.Properties.Remove(property);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new PropertyDeleteResult(true, false);
    }
}
