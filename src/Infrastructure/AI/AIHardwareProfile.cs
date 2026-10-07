using System.Diagnostics;

namespace RPGBattleMaker.Infrastructure.AI;

public enum AIBackend
{
    Cuda,
    Vulkan,
    Cpu
}

public sealed record AIHardwareProfile(
    AIBackend Backend,
    int VramMb,
    uint ContextSize,
    uint BatchSize,
    uint UBatchSize,
    int GpuLayerCount,
    bool FlashAttention,
    string Description)
{
    public static AIHardwareProfile Detect()
    {
        if (TryGetNvidiaVram(out int vramMb))
        {
            if (vramMb >= 6144)
            {
                return new(
                    AIBackend.Cuda,
                    vramMb,
                    4096,
                    1024,
                    512,
                    99,
                    true,
                    $"NVIDIA/CUDA {vramMb} MB VRAM");
            }

            if (vramMb >= 4096)
            {
                return new(
                    AIBackend.Cuda,
                    vramMb,
                    4096,
                    512,
                    256,
                    99,
                    true,
                    $"NVIDIA/CUDA {vramMb} MB VRAM");
            }

            return new(
                AIBackend.Cuda,
                vramMb,
                2048,
                256,
                128,
                99,
                true,
                $"NVIDIA/CUDA {vramMb} MB VRAM");
        }

        return new(
            AIBackend.Vulkan,
            0,
            4096,
            256,
            128,
            99,
            true,
            "Vulkan GPU (VRAM não identificada automaticamente)");
    }

    public static AIHardwareProfile CreateVulkan()
    {
        return new(
            AIBackend.Vulkan,
            0,
            4096,
            256,
            128,
            99,
            true,
            "Vulkan GPU (VRAM não identificada automaticamente)");
    }

    public static AIHardwareProfile CreateCpu()
    {
        return new(
            AIBackend.Cpu,
            0,
            2048,
            128,
            64,
            0,
            false,
            "CPU");
    }

    private static bool TryGetNvidiaVram(out int vramMb)
    {
        vramMb = 0;

        try
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=memory.total --format=csv,noheader,nounits",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1500);

            if (process.ExitCode != 0)
                return false;

            string firstLine = output
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? string.Empty;

            return int.TryParse(
                firstLine.Trim(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out vramMb);
        }
        catch
        {
            return false;
        }
    }
}
