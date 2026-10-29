using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HonorAdbPackageManager.ViewModels;

/// <summary>INotifyPropertyChanged 基类</summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
