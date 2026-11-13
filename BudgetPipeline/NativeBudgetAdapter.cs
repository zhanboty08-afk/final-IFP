using System.Runtime.InteropServices;

namespace BudgetPipeline;

/// <summary>Boundary adapter for the platform-specific native C budget utility.</summary>
public static class NativeBudgetAdapter
{
    private const string LibraryName = "budget_native";

    static NativeBudgetAdapter()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeBudgetAdapter).Assembly, ResolveLibrary);
    }

    public static Result<decimal> UtilizationPercent(decimal spent, decimal limit)
    {
        try
        {
            var status = CalculateUtilizationPercent((double)spent, (double)limit, out var percentage);
            if (status != 0)
            {
                var reason = status switch
                {
                    1 => "The native function requires a positive budget limit.",
                    2 => "The native function requires a finite, non-negative spent amount.",
                    3 => "The native function received an invalid output pointer.",
                    _ => $"The native function returned unexpected status {status}."
                };

                return Result<decimal>.Failure(Errors.NativeIntegration($"status {status}: {reason}"));
            }

            return Result<decimal>.Success((decimal)percentage);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException
                                       or BadImageFormatException)
        {
            return Result<decimal>.Failure(Errors.NativeIntegration(ex.Message));
        }
    }

    private static IntPtr ResolveLibrary(string libraryName, System.Reflection.Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
            return IntPtr.Zero;

        var (runtime, fileName) = OperatingSystem.IsWindows()
            ? ("win-x64", "budget_native.dll")
            : OperatingSystem.IsLinux()
                ? ("linux-x64", "libbudget_native.so")
                : (string.Empty, string.Empty);

        if (runtime.Length == 0)
            return IntPtr.Zero;

        var path = Path.Combine(AppContext.BaseDirectory, "runtimes", runtime, "native", fileName);
        return File.Exists(path) ? NativeLibrary.Load(path) : IntPtr.Zero;
    }

    [DllImport(LibraryName, EntryPoint = "calculate_utilization_percent",
        CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int CalculateUtilizationPercent(double spent, double limit,
        out double percentage);
}
