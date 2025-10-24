using LiaXP.Domain.Entities;

namespace LiaXP.Domain.Interfaces;

public interface ISalesDataSource
{
    Task<IEnumerable<Sale>> GetSalesByCompanyAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<Sale>> GetSalesBySellerAsync(Guid sellerId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<Sale>> GetSalesByStoreAsync(Guid storeId, DateTime? startDate = null, DateTime? endDate = null);
    Task UpsertSalesAsync(IEnumerable<Sale> sales);
    
    Task<IEnumerable<Goal>> GetGoalsByCompanyAsync(Guid companyId, DateTime? month = null);
    Task<IEnumerable<Goal>> GetGoalsBySellerAsync(Guid sellerId, DateTime? month = null);
    Task UpsertGoalsAsync(IEnumerable<Goal> goals);
    
    Task<Company?> GetCompanyByCodeAsync(string code);
    Task<Store?> GetStoreByNameAsync(Guid companyId, string name);
    Task<Seller?> GetSellerByCodeAsync(Guid companyId, string sellerCode);
    Task<Seller?> GetSellerByPhoneAsync(Guid companyId, string phoneE164);
    
    Task UpsertCompanyAsync(Company company);
    Task UpsertStoreAsync(Store store);
    Task UpsertSellerAsync(Seller seller);
}
