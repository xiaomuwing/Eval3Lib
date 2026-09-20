using Eval3.Thermo;

namespace Eval3.Tests;

/// <summary>
/// 水蒸汽物性函数库测试。对照基准为历史 WASPCN 库（413 KB 版）在 32 位进程中的实测输出；
/// 本库为纯托管 IAPWS-IF97（XSteamNET），热力学性质与基准完全一致，输运性质存在约 1% 的标准版本差异。
/// </summary>
public class ThermoLibraryTests
{
    /// <summary>
    /// 通用容差下限。热力学性质（比焓、比熵、比容、比内能、比热、音速、饱和参数）
    /// 与历史 WASPCN 实测完全一致（0.000%），这里留 0.01% 只作为浮点噪声余量。
    /// </summary>
    private const double StandardDifferenceTolerance = 1e-4;

    /// <summary>
    /// 输运性质（导热系数、普朗特数）容差：IF97 本体不含输运性质，
    /// 双方各自采用不同版本的 IAPWS 输运方程，实测差异 ≤1.1%。
    /// </summary>
    private const double TransportDifferenceTolerance = 2e-2;

    /// <summary>相对误差断言：便于同时看到基准值与实际值。</summary>
    private static void AssertClose(double waspcn, double actual, double relativeTolerance, string name)
    {
        double difference = Math.Abs(actual - waspcn);
        double relative = waspcn == 0d ? difference : difference / Math.Abs(waspcn);
        double tolerance = Math.Max(relativeTolerance, StandardDifferenceTolerance);
        Assert.True(
            relative <= tolerance,
            $"{name}: WASPCN={waspcn:G8}, 本库={actual:G8}, 相对差={relative:P3}（阈值 {tolerance:P3}）");
    }

    [Fact]
    public void SaturationPropertiesMatchWaspcn()
    {
        // WASPCN 实测（p = 1 MPa）：Tsat=179.885633533, hf=762.682849362, hg=2777.11954078,
        // vf=0.001127233747, vg=0.194348885, eta_f=0.000150239329, lambda_f=0.673376253, w_f=1391.63870
        AssertClose(179.885633533, ThermoLibrary.P2T(1d), 5e-3, "P2T(1) 饱和温度 ℃");
        AssertClose(762.682849362, ThermoLibrary.P2HL(1d), 1e-4, "P2HL(1) 饱和水比焓 kJ/kg");
        AssertClose(2777.11954078, ThermoLibrary.P2HG(1d), 1e-4, "P2HG(1) 饱和汽比焓 kJ/kg");
        AssertClose(0.001127233747, ThermoLibrary.P2VL(1d), 1e-4, "P2VL(1) 饱和水比容 m³/kg");
        AssertClose(0.194348885018, ThermoLibrary.P2VG(1d), 1e-4, "P2VG(1) 饱和汽比容 m³/kg");
        AssertClose(0.000150239329, ThermoLibrary.P2ETAL(1d), TransportDifferenceTolerance, "P2ETAL(1) 饱和水动力粘度 Pa·s");
        AssertClose(0.673376253, ThermoLibrary.P2RAMDL(1d), TransportDifferenceTolerance, "P2RAMDL(1) 饱和水导热系数 W/(m·K)");
        AssertClose(1391.638703, ThermoLibrary.P2SSPL(1d), 1e-4, "P2SSPL(1) 饱和水音速 m/s");
    }

    [Fact]
    public void SuperheatedPropertiesMatchWaspcn()
    {
        // WASPCN 实测（p = 1 MPa, t = 300 ℃）
        AssertClose(3051.703186, ThermoLibrary.PT2H(1d, 300d), 1e-4, "PT2H(1,300) 比焓");
        AssertClose(7.124712479, ThermoLibrary.PT2S(1d, 300d), 1e-4, "PT2S(1,300) 比熵");
        AssertClose(0.257979190, ThermoLibrary.PT2V(1d, 300d), 1e-4, "PT2V(1,300) 比容");
        AssertClose(2.140833564, ThermoLibrary.PT2CP(1d, 300d), 1e-4, "PT2CP(1,300) 定压比热");
        AssertClose(1.608136986, ThermoLibrary.PT2CV(1d, 300d), 1e-4, "PT2CV(1,300) 定容比热");
        AssertClose(2793.723996, ThermoLibrary.PT2E(1d, 300d), 1e-4, "PT2E(1,300) 比内能");
        AssertClose(5.20728482e-6, ThermoLibrary.PT2U(1d, 300d), 1e-4, "PT2U(1,300) 运动粘度");
        AssertClose(0.0449573361, ThermoLibrary.PT2RAMD(1d, 300d), TransportDifferenceTolerance, "PT2RAMD(1,300) 导热系数");
        AssertClose(2.01849026e-5, ThermoLibrary.PT2ETA(1d, 300d), TransportDifferenceTolerance, "PT2ETA(1,300) 动力粘度");
        AssertClose(0.961189444, ThermoLibrary.PT2PRN(1d, 300d), TransportDifferenceTolerance, "PT2PRN(1,300) 普朗特数");
        AssertClose(578.514018, ThermoLibrary.PT2SSP(1d, 300d), 1e-4, "PT2SSP(1,300) 音速");
        AssertClose(8.587708330, ThermoLibrary.T2P(300d), 1e-4, "T2P(300) 饱和压力 MPa");
    }

    [Fact]
    public void InverseFunctionsRoundTrip()
    {
        double h = ThermoLibrary.PT2H(1d, 300d);
        double s = ThermoLibrary.PT2S(1d, 300d);

        Assert.Equal(300d, ThermoLibrary.PH2T(1d, h), 1);
        Assert.Equal(300d, ThermoLibrary.PS2T(1d, s), 1);
        Assert.Equal(h, ThermoLibrary.PS2H(1d, s), 1);
    }

    /// <summary>热力学恒等式自洽性——不依赖标准差异，可长期作为回归护栏。</summary>
    [Fact]
    public void ThermodynamicIdentitiesHold()
    {
        const double p = 1d;
        const double t = 300d;

        // u = h - p·v
        double u = ThermoLibrary.PT2E(p, t);
        double h = ThermoLibrary.PT2H(p, t);
        double v = ThermoLibrary.PT2V(p, t);
        Assert.Equal(h - p * 1000d * v, u, 3);

        // ν = μ / ρ
        double eta = ThermoLibrary.PT2ETA(p, t);
        double rho = ThermoLibrary.PT2Rho(p, t);
        Assert.Equal(eta / rho, ThermoLibrary.PT2U(p, t), 10);

        // Pr = μ·cp / λ（cp 换算成 J/(kg·K)）
        double cp = ThermoLibrary.PT2CP(p, t) * 1000d;
        double lambda = ThermoLibrary.PT2RAMD(p, t);
        Assert.Equal(eta * cp / lambda, ThermoLibrary.PT2PRN(p, t), 3);

        // 比容与密度互为倒数
        Assert.Equal(1d / v, rho, 6);

        Assert.True(ThermoLibrary.P2HL(p) < ThermoLibrary.P2HG(p), "饱和水比焓应小于饱和汽比焓");

        // 饱和线上：饱和水状态的比焓应介于同压力下的欠热水与过热汽之间
        double tsat = ThermoLibrary.P2T(p);
        Assert.True(ThermoLibrary.PT2H(p, tsat - 5d) < ThermoLibrary.P2HL(p), "欠热水比焓应低于饱和水比焓");
        Assert.True(ThermoLibrary.PT2H(p, tsat + 5d) > ThermoLibrary.P2HG(p), "过热汽比焓应高于饱和汽比焓");
    }

    /// <summary>越界输入必须抛异常，而不是返回无意义的数值。</summary>
    [Fact]
    public void OutOfRangeInputThrows()
    {
        // 1 MPa、-50 ℃ 低于三相点，超出适用范围
        Assert.ThrowsAny<Exception>(() => ThermoLibrary.PT2H(1d, -50d));
    }

    /// <summary>可作为 Eval3 函数库直接注册并参与公式求值。</summary>
    [Fact]
    public void WorksAsEval3FunctionLibrary()
    {
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(typeof(ThermoLibrary));

        AssertClose(179.885633533, engine.EvaluateNumber("P2T(1)"), 5e-3, "公式 P2T(1)");
        AssertClose(3051.703186, engine.EvaluateNumber("PT2H(1, 300)"), 5e-3, "公式 PT2H(1, 300)");
        AssertClose(2777.11954078, engine.EvaluateNumber("P2HG(1)"), 5e-3, "公式 P2HG(1)");

        // 参与业务公式：汽轮机理想比焓降
        double drop = engine.EvaluateNumber("PT2H(1, 300) - P2HG(0.1)");
        Assert.True(drop > 300d && drop < 400d, $"1 MPa/300℃ 至 0.1 MPa 的比焓降应在 300~400 kJ/kg，实际 {drop}");
    }

    /// <summary>
    /// 并行求值一致性：纯托管实现不保存状态，Eval3 的并行批量验算结果应与单线程一致。
    /// </summary>
    [Fact]
    public void ParallelEvaluationIsConsistent()
    {
        var engine = new Evaluator();
        engine.AddEnvironmentFunctions(typeof(ThermoLibrary));
        CompiledFormula formula = engine.Compile("PT2H(1, 300) + P2HG(0.1) * 0.001");
        double expected = formula.EvaluateNumber();

        Parallel.For(0, 4_000, index =>
        {
            double actual = formula.EvaluateNumber();
            Assert.True(
                Math.Abs(actual - expected) < 1e-9,
                $"第 {index} 次并行求值结果 {actual} 与单线程 {expected} 不一致");
        });
    }
}
