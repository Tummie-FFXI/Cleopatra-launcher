using System.Windows;
using System.Windows.Input;

namespace Cleopatra.GUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // ----------------------------------------------------
    // WINDOW DRAG
    // ----------------------------------------------------

    private void Header_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();

            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    // ----------------------------------------------------
    // MINIMIZE
    // ----------------------------------------------------

    private void MinimizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState =
            WindowState.Minimized;
    }

    // ----------------------------------------------------
    // MAXIMIZE / RESTORE
    // ----------------------------------------------------

    private void MaximizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void ToggleMaximize()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState =
                WindowState.Normal;
        }
        else
        {
            WindowState =
                WindowState.Maximized;
        }
    }

    // ----------------------------------------------------
    // CLOSE
    // ----------------------------------------------------

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}