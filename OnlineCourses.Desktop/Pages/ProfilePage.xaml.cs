using System.Windows.Controls;
using OnlineCourses.Client.Models;
using OnlineCourses.Desktop.ViewModels;

namespace OnlineCourses.Desktop.Pages;

public partial class ProfilePage : Page
{
    public ProfilePage(CurrentUserDto user)
    {
        InitializeComponent();
        DataContext = new ProfileViewModel(user);
    }
}
