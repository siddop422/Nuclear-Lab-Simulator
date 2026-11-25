# Nuclear Lab Simulator

A lightweight, offline Windows desktop app with two independent educational simulation labs.

## Labs

- **Nuclear Fusion Lab** — adjust plasma temperature, fuel density, confinement time, magnetic field and deuterium share. Watch an illustrative fusion-performance estimate, Q value, stability score and live output trace.
- **Black Hole Observatory** — adjust black-hole mass, dimensionless spin and accretion activity. Explore a schematic accretion disk and inspect approximate horizon scale, Hawking temperature and orbital period.

The overview is a launcher. Open either lab, switch between them, start/pause an experiment or return to the overview. The app runs locally and makes no network requests.

## Download

- **[Latest Windows releases](https://github.com/siddop422/Nuclear-Lab-Simulator/releases)** — download `NuclearLabSimulator.exe` when a release is available.
- **[Windows build artifacts](https://github.com/siddop422/Nuclear-Lab-Simulator/actions)** — download `NuclearLabSimulator-windows-x64` from a successful workflow run.
- **[Download source ZIP](https://github.com/siddop422/Nuclear-Lab-Simulator/archive/refs/heads/main.zip)**

The Windows EXE is built as a self-contained, single-file x64 application. It does not require a separately installed .NET runtime.

## Run from source

Requirements: Windows 10/11 and the .NET 8 SDK.

```powershell
dotnet run --project .\NuclearLabSimulator.csproj
```

## Build a standalone EXE

```powershell
dotnet publish .\NuclearLabSimulator.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o .\publish
```

The output is `publish\NuclearLabSimulator.exe`.

## Automated Windows builds

GitHub Actions builds on every push to `main` and on manual workflow dispatch. Each successful build uploads the Windows executable as an artifact.

To publish a versioned GitHub Release, tag a commit and push the tag:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

The workflow builds the EXE and attaches it to that release.

## Model limitations

This is an educational visualization, **not a validated scientific solver**. It must not be used for engineering, safety, investment or research decisions.

- The fusion lab uses a simplified normalized performance model. It does not solve plasma transport, magnetohydrodynamics, neutron transport, reactor engineering constraints or real Lawson-criterion operating envelopes. Its Q value is illustrative, not a prediction of an actual reactor.
- The black-hole lab uses the Schwarzschild-radius relationship and simplified orbital calculations. The accretion disk is schematic; this version does not solve the Kerr metric, ray tracing or general-relativistic radiative transfer.
- Displayed values are approximate teaching aids. The visualizations are not physical-scale renderings.

## License

MIT. See [LICENSE](LICENSE).
