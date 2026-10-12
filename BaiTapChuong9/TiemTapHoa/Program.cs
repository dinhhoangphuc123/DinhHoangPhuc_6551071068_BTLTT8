using System.Globalization;

namespace TiemTapHoa;

static class Program
{
    [STAThread]
    static void Main()
    {
        var vn = new CultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = vn;
        CultureInfo.CurrentCulture = vn;
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
