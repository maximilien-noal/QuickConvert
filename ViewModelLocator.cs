namespace QuickConvert
{
    using GalaSoft.MvvmLight.Ioc;

    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            SimpleIoc.Default.Register(() => new MainViewModel());
        }
        public MainViewModel Main => SimpleIoc.Default.GetInstance<MainViewModel>();

    }
}