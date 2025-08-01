using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Palermo.BlazorMvc;
using ProgrammingWithPalermo.ChurchBulletin.Core.Model;

namespace UI.Shared.Pages;

[Route("/fetchchurchbulletin")]
public class FetchChurchBulletinController : ControllerComponentBase<FetchChurchBulletinView>
{
    private ChurchBulletinItem[]? _bulletins;
    [Inject] public HttpClient? Http { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("FetchChurchBulletinController");
        Debug.Assert(Http != null, nameof(Http) + " != null");
        _bulletins = await Http.GetFromJsonAsync<ChurchBulletinItem[]>("ChurchBulletinItem");
        View.Model = _bulletins;
    }
}