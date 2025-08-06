using IksAdmin.Api.Entities.Admins;

namespace IksAdmin.Api.Application.Admins;

internal class AdminsService : IAdminsService
{
    private readonly IAdminsRepository _adminsRepository;

    public AdminsService(IAdminsRepository adminsRepository)
    {
        _adminsRepository = adminsRepository;
    }

    public IAdminsRepository GetRepository()
    {
        return _adminsRepository;
    }

    public async Task<IEnumerable<Admin>> GetAdminsAsync()
    {
        return await _adminsRepository.GetAllAsync();
    }

    public IEnumerable<Admin> GetStoredAdmins()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Admin> GetOnlineAdmins()
    {
        throw new NotImplementedException();
    }

    public Admin GetById(int id)
    {
        throw new NotImplementedException();
    }
}