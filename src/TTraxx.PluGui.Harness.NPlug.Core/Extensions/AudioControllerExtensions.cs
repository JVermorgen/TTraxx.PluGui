using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Core.Extensions;

/// <summary>
/// NPlug controller helpers for the harness.
/// </summary>
public static class AudioControllerExtensions
{
    /// <summary>
    /// Sets every parameter in the controller's model to the default its own info declares, each as a
    /// properly bracketed begin/end edit.
    ///
    /// The harness needs this because nothing else establishes an initial state: in a DAW the host
    /// loads a preset or its saved state right after creating the controller, whereas the harness
    /// creates one and goes straight to opening the editor. Running the values through the controller's
    /// edit path rather than assigning them directly means the GUI is brought up the same way a real
    /// parameter change would bring it up.
    /// </summary>
    public static void ReassertParameterDefaults<TAudioControllerModel>(this AudioController<TAudioControllerModel> controller)
        where TAudioControllerModel : AudioProcessorModel, new()
    {
        for (int i = 0; i < controller.Model.ParameterCount; i++)
        {
            AudioParameter param = controller.Model.GetParameterByIndex(i);
            controller.BeginEditParameter(param);
            param.NormalizedValue = param.GetInfo().DefaultNormalizedValue;
            controller.EndEditParameter();
        }
    }
}
