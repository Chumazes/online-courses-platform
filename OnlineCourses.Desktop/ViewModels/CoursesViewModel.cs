using System.Collections.ObjectModel;
using System.Windows.Input;
using OnlineCourses.Desktop.Infrastructure;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class CoursesViewModel : ViewModelBase
{
    private readonly Action<CourseCardViewModel> _openCourse;
    private readonly Func<Task> _logout;
    private CourseCardViewModel? _selectedCourse;

    public CoursesViewModel(Action<CourseCardViewModel> openCourse, Func<Task> logout)
    {
        _openCourse = openCourse;
        _logout = logout;

        OpenCourseCommand = new RelayCommand(
            _ => OpenSelectedCourse(),
            _ => SelectedCourse is not null);

        LogoutCommand = new AsyncRelayCommand(_logout);

        Courses = new ObservableCollection<CourseCardViewModel>(BuildDemoCourses());
    }

    public ObservableCollection<CourseCardViewModel> Courses { get; }

    public CourseCardViewModel? SelectedCourse
    {
        get => _selectedCourse;
        set
        {
            if (SetProperty(ref _selectedCourse, value))
            {
                ((RelayCommand)OpenCourseCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand OpenCourseCommand { get; }
    public ICommand LogoutCommand { get; }

    private void OpenSelectedCourse()
    {
        if (SelectedCourse is not null)
        {
            _openCourse(SelectedCourse);
        }
    }

    private static IReadOnlyList<CourseCardViewModel> BuildDemoCourses()
    {
        return
        [
            new CourseCardViewModel
            {
                Id = 1,
                Title = "C# Fundamentals",
                Description = "Основы языка, коллекции, LINQ и ООП.",
                Level = "beginner",
                Price = 0
            },
            new CourseCardViewModel
            {
                Id = 2,
                Title = "ASP.NET Core API",
                Description = "Контроллеры, JWT, EF Core и практический backend.",
                Level = "intermediate",
                Price = 1490
            },
            new CourseCardViewModel
            {
                Id = 3,
                Title = "WPF Desktop Basics",
                Description = "MVVM-подход, команда, биндинги, навигация.",
                Level = "beginner",
                Price = 990
            }
        ];
    }
}
