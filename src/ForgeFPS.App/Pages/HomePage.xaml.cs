using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ForgeFPS.Application;
using System;
using System.Threading.Tasks;

namespace ForgeFPS.App.Pages;

public sealed partial class HomePage : Page
{
    private readonly DiagnosticService _diagnostic;

    public HomePage()
    {
        InitializeComponent();
        _diagnostic = App.Services.GetRequiredService<DiagnosticService>();
    }

    private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Detectando hardware...";
        ProgressBar.Visibility = Visibility.Visible;
        ProgressBar.IsIndeterminate = true;

        try
        {
            var hardware = await _diagnostic.DetectHardwareAsync();
            var games = await _diagnostic.DetectGamesAsync();
            StatusText.Text = $"Hardware detectado: {hardware.Cpus.Count} CPU(s), {hardware.Gpus.Count} GPU(s), {hardware.TotalMemoryBytes / (1024 * 1024 * 1024)} GB RAM. Jogos: {games.Count}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Erro: " + ex.Message;
        }
        finally
        {
            ProgressBar.Visibility = Visibility.Collapsed;
        }
    }
}
