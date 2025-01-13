using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IksAdminApi;

public class KickReason
{
    public string Title {get; set;} // Причина отображаемая в меню
    public string Text {get; set;} // Причина отображаемая при бане
    public bool HideFromMenu {get; set;} = false; // Скрыть ли причину из меню(по какой то причине)

    public KickReason(string title, string? text = null, bool hideFromMenu = false)
    {
        Title = title;
        if (text == null)
            Text = title;
        else Text = text;
        HideFromMenu = hideFromMenu;
    }
}