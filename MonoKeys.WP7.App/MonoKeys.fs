namespace WindowsPhoneApp

open System
open System.Net
open System.Windows
open System.Windows.Controls
open System.Windows.Documents
open System.Windows.Ink
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Animation
open System.Windows.Shapes
open System.Windows.Navigation
open Microsoft.Phone.Controls
open Microsoft.Phone.Shell
open Microsoft.Xna.Framework.Audio

type View () as this =
    inherit UserControl()
    
    let sampleRate = 44100
    let sampleLength = 1.5 * float sampleRate |> int
    let sample x = x * 32767. |> int16

    let toBytes (xs:int16[]) =
        let bytes = Array.CreateInstance(typeof<byte>, 2 * xs.Length)
        Buffer.BlockCopy(xs, 0, bytes, 0, 2*xs.Length)
        bytes :?> byte[]

    let pi = Math.PI
    let sineWave freq i = sin (pi * 2. * float i / float sampleRate * freq)
    let fadeOut i = float (sampleLength-i) / float sampleLength
    let tremolo freq depth i = (1.0 - depth) + depth * (sineWave freq i) ** 2.0

    let create f =
        Array.init sampleLength (f >> min 1.0 >> max -1.0 >> sample)
        |> toBytes

    let play bytes =
        let effect = new SoundEffect(bytes, sampleRate, AudioChannels.Mono)       
        effect.Play() |> ignore
    
    let tones = [
        "A", 220.00
        "A#", 233.08
        "B", 246.94
        "C", 261.63
        "C#", 277.18
        "D", 293.66
        "D#", 311.13
        "E", 329.63
        "F", 349.23
        "F#", 369.99
        "G", 392.00
        "G#", 415.30
        "A", 440.00
        "A#", 466.16
        "B", 493.88
        "C", 523.25
        "C#", 554.37
        "D", 587.33]

    let grid = Grid()
    do  grid.RowDefinitions.Add(RowDefinition(Height=GridLength.Auto))
    do  grid.RowDefinitions.Add(RowDefinition())
    do  grid.RowDefinitions.Add(RowDefinition())

    let tremoloFreq = Slider(Minimum = 0.001, Maximum = 10.0, Value=2.0)
    do  Grid.SetRow(tremoloFreq, 1)
    let tremoloDepth = Slider(Minimum = 0.0, Maximum = 50.0, Value=10.0)
    do  Grid.SetRow(tremoloDepth, 2)

    let keys =
        tones 
        |> Seq.skip 4 |> Seq.take 6 
        |> Seq.map (fun (text,freq) ->
            let key = Button(Content=text)
            key.Margin <- Thickness(1.0)      
            key.Click.Add(fun _ ->
                let tremolo i = tremolo tremoloFreq.Value tremoloDepth.Value i
                let shape i = sineWave freq i * fadeOut i * tremolo i
                let bytes = create shape
                play bytes
            )
            key
        )

    let keyGrid = Grid(Height=128.)
    do  keys |> Seq.iteri (fun i key ->
            keyGrid.ColumnDefinitions.Add(ColumnDefinition())
            Grid.SetColumn(key,i)
            keyGrid.Children.Add key
        )

    do  grid.Children.Add keyGrid
    do  grid.Children.Add tremoloFreq
    do  grid.Children.Add tremoloDepth

    do  this.Content <- grid

type MainPage () as page =
    inherit PhoneApplicationPage()
    // Load the Xaml for the page.
    do Application.LoadComponent(page, new System.Uri("/WindowsPhoneApp;component/MainPage.xaml", System.UriKind.Relative))
    do page.Content <- View()
    override page.OnOrientationChanged args =
        base.OnOrientationChanged args

/// One instance of this type is created in the application host project.
type App(app:Application) = 
    // Global handler for uncaught exceptions. 
    // Note that exceptions thrown by ApplicationBarItem.Click will not get caught here.
    do app.UnhandledException.Add(fun e -> 
        if (System.Diagnostics.Debugger.IsAttached) then
            // An unhandled exception has occurred, break into the debugger
            System.Diagnostics.Debugger.Break();
     )

    let rootFrame = new PhoneApplicationFrame()
    
    do app.RootVisual <- rootFrame;

    // Handle navigation failures
    do rootFrame.NavigationFailed.Add(fun _ -> 
        if (System.Diagnostics.Debugger.IsAttached) then
            // A navigation has failed; break into the debugger
            System.Diagnostics.Debugger.Break())

    //do rootFrame.Content <- MainPage(View())
    // Navigate to the main page 
    do rootFrame.Navigate(new Uri("/WindowsPhoneApp;component/MainPage.xaml", UriKind.Relative)) |> ignore

    // Required object that handles lifetime events for the application
    let service = PhoneApplicationService()
    // Code to execute when the application is launching (eg, from Start)
    // This code will not execute when the application is reactivated
    do service.Launching.Add(fun _ -> ())
    // Code to execute when the application is closing (eg, user hit Back)
    // This code will not execute when the application is deactivated
    do service.Closing.Add(fun _ -> ())
    // Code to execute when the application is activated (brought to foreground)
    // This code will not execute when the application is first launched
    do service.Activated.Add(fun _ -> ())
    // Code to execute when the application is deactivated (sent to background)
    // This code will not execute when the application is closing
    do service.Deactivated.Add(fun _ -> ())

    do app.ApplicationLifetimeObjects.Add(service) |> ignore
