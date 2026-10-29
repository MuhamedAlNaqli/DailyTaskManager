using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Avalonia.Services;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Localization;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Avalonia.ViewModels;

/// <summary>"My business" in Settings: the consultant's own company, signature and stamp</summary>
public partial class BusinessViewModel : ObservableObject
{
    private static readonly Dictionary<string, List<string>> ImageFiles = new()
    {
        ["Images (PNG, JPEG)"] = new List<string> { "*.png", "*.jpg", "*.jpeg" }
    };

    private readonly IBusinessService _business;
    private readonly IExportService _exportService;
    private readonly IPlatformService _platformService;
    private readonly DialogService _dialogService;
    private readonly ILogger<BusinessViewModel> _logger;

    [ObservableProperty] private string _companyName = "";
    [ObservableProperty] private string _companyId = "";
    [ObservableProperty] private string _vatNumber = "";
    [ObservableProperty] private string _addressLine1 = "";
    [ObservableProperty] private string _addressLine2 = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _decisionNumber = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSignature))]
    private IImage? _signaturePreview;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStamp))]
    private IImage? _stampPreview;

    [ObservableProperty] private bool _isBusy;

    public bool HasSignature => SignaturePreview != null;

    public bool HasStamp => StampPreview != null;

    public BusinessViewModel(IBusinessService business, IExportService exportService, IPlatformService platformService,
        DialogService dialogService, ILogger<BusinessViewModel> logger)
    {
        _business = business;
        _exportService = exportService;
        _platformService = platformService;
        _dialogService = dialogService;
        _logger = logger;

        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        try
        {
            var profile = await _business.GetProfileAsync();
            CompanyName = profile.CompanyName;
            CompanyId = profile.CompanyId;
            VatNumber = profile.VatNumber;
            AddressLine1 = profile.AddressLine1;
            AddressLine2 = profile.AddressLine2;
            Phone = profile.Phone;
            Email = profile.Email;
            DecisionNumber = profile.DecisionNumber;
            SignaturePreview = LoadImage(profile.SignatureImagePath);
            StampPreview = LoadImage(profile.StampImagePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load the business profile");
        }
    }

    private static IImage? LoadImage(string path)
    {
        try
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path) ? new Bitmap(path) : null;
        }
        catch
        {
            return null;
        }
    }

    private BusinessProfile ToProfile() => new()
    {
        CompanyName = CompanyName,
        CompanyId = CompanyId,
        VatNumber = VatNumber,
        AddressLine1 = AddressLine1,
        AddressLine2 = AddressLine2,
        Phone = Phone,
        Email = Email,
        DecisionNumber = DecisionNumber
    };

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            await _business.SaveProfileAsync(ToProfile());
            await _dialogService.ShowInfoDialogAsync("Saved", "Your business details were saved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving the business profile");
            await _dialogService.ShowErrorDialogAsync("Error", $"Could not save your business details: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task ChooseSignatureAsync() => ChooseImageAsync("Choose your signature image", p => _business.SetSignatureImageAsync(p), image => SignaturePreview = image);

    [RelayCommand]
    private Task ChooseStampAsync() => ChooseImageAsync("Choose your stamp image", p => _business.SetStampImageAsync(p), image => StampPreview = image);

    [RelayCommand]
    private async Task RemoveSignatureAsync()
    {
        await _business.RemoveSignatureImageAsync();
        SignaturePreview = null;
    }

    [RelayCommand]
    private async Task RemoveStampAsync()
    {
        await _business.RemoveStampImageAsync();
        StampPreview = null;
    }

    private async Task ChooseImageAsync(string title, Func<string, Task<string>> store, Action<IImage?> show)
    {
        try
        {
            var path = await _platformService.ShowOpenFileDialogAsync(Loc.T(title), ImageFiles);
            if (string.IsNullOrEmpty(path)) return;

            IsBusy = true;
            show(LoadImage(await store(path)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not use the chosen image");
            await _dialogService.ShowErrorDialogAsync("Image", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PreviewAsync()
    {
        try
        {
            IsBusy = true;
            await _business.SaveProfileAsync(ToProfile());
            var settings = await _business.GetDocumentSettingsAsync(null, DateTime.Today);
            await ClientsViewModel.ShowSampleWorkOrderAsync(_exportService, _platformService, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating the preview");
            await _dialogService.ShowErrorDialogAsync("Error", $"Could not create the preview: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
