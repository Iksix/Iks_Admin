using IksAdmin.Api.Contracts.Bans;

namespace IksAdmin.Api.Application.Comms;

public interface ICommsService
{
    /// <summary>
    /// Give ban to player
    /// </summary>
    /// <returns>Ban Id in DataBase</returns>
    Task<int> GiveBanAsync(GiveBanDto giveBanDto);
}