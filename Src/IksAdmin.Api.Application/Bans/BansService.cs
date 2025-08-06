using CounterStrikeSharp.API;
using IksAdmin.Api.Application.Admins;
using IksAdmin.Api.Application.Bans.Events;
using IksAdmin.Api.Application.Events;
using IksAdmin.Api.Contracts.Bans;
using IksAdmin.Api.Entities.Admins;
using IksAdmin.Api.Entities.Bans;
using Microsoft.Extensions.Logging;

namespace IksAdmin.Api.Application.Bans;

internal class BansService
{
    private readonly IBansRepository _bansRepository;
    private readonly IAdminsService _adminsService;
    private readonly ILogger _logger;


    public BansService(IBansRepository bansRepository, IAdminsService adminsService, ILogger logger)
    {
        _bansRepository = bansRepository;
        _adminsService = adminsService;
        _logger = logger;
    }


    public async Task<Ban> GiveAsync(GiveBanDto giveBanDto)
    {
        var newBan = new Ban
        {
            AdminId = giveBanDto.AdminId,
            Reason = giveBanDto.Reason,
            Duration = giveBanDto.Duration,
            BanType = giveBanDto.BanType,
            SteamId = giveBanDto.SteamId,
            Ip = giveBanDto.Ip
        };
        
        var onBanEventData = new BansEvents.OnBan
        {
            Admin = _adminsService.GetById(giveBanDto.AdminId),
            Ban = newBan,
            Announce = giveBanDto.Announce,
            KickPlayer = true
        };

        await EventManager.InvokeAsync(onBanEventData, async void () =>
        {
            await _bansRepository.AddAsync(onBanEventData.Ban);
            _logger.LogInformation("Add ban {@onBanEventData}", onBanEventData);
            
            await Server.NextFrameAsync(() =>
            {
                ExecuteBan(onBanEventData.Admin, onBanEventData.Ban, onBanEventData.Announce, onBanEventData.KickPlayer);
            });
        });
        
        return newBan;
    }
    

    /// <summary>
    /// Executes ban for online player
    /// </summary>
    public void ExecuteBan(Admin admin, Ban ban, bool announce = true, bool kickPlayer = true)
    {
        // TODO: Реализация
    }
    
    
}