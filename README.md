# Nuclear Lab Simulator

**Nuclear Lab Simulator** is a lightweight, offline Windows desktop application with two independent educational simulation labs:

- **Nuclear Fusion Lab** — adjust deuterium/tritium fuel mix, plasma temperature, density, confinement time and magnetic field. Explore a simplified fusion-power estimate, energy-gain factor (Q), plasma-stability indicator and accumulated energy.
- **Black Hole Observatory** — explore black-hole mass, dimensionless spin and accretion activity. View an animated accretion-disk illustration and inspect Schwarzschild radius, a reference orbital clock-rate factor, orbital period and Hawking temperature.

The overview acts as a launcher: open either lab, switch between them, pause or reset an experiment. The app runs locally and makes no network requests.

## Download

Open **[Releases](https://github.com/siddop422/Nuclear-Lab-Simulator/releases)** and download `NuclearLabSimulator.exe` when a release is available.

The Windows build is published as a **self-contained, single-file x64 executable**. The .NET runtime does not need to be installed separately.

Download the complete [source project archive](https://raw.githubusercontent.com/siddop422/Nuclear-Lab-Simulator/main/Nuclear-Lab-Simulator-source.zip) to inspect or build the C# project locally.

For builds from the latest source, open **[Actions](https://github.com/siddop422/Nuclear-Lab-Simulator/actions)** and download the `NuclearLabSimulator-windows-x64` artifact from a successful run.

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

GitHub Actions builds the application on pushes to `main` and on manual workflow dispatch. Each successful build uploads a downloadable Windows x64 executable artifact.

To publish a versioned GitHub Release, create and push a tag such as:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

The workflow then builds the EXE and attaches it to the matching GitHub Release.

## Model notes and limitations

This is an **educational visualization**, not a validated scientific solver. It is designed to make a few relationships intuitive and should not be used for engineering, safety, investment or research decisions.

- The fusion lab uses a deliberately simplified normalized performance model. It does not solve plasma transport equations, magnetohydrodynamics, neutron transport, reactor engineering constraints or real Lawson-criterion operating envelopes. Its Q value is an illustrative model output, not a prediction of a real reactor.
- The black-hole lab uses the Schwarzschild-radius relationship and a simplified orbital reference calculation. The disk is a visual representation; spin influences the illustration but the app does not solve the Kerr metric, ray-tracing or general-relativistic radiative transfer.
- All displayed values are approximate teaching aids. The animated graphics are schematic, not physical-scale renderings.

## License

MIT. See `LICENSE`.
