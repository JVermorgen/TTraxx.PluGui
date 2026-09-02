using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Core.Extensions;

public static class AudioControllerExtensions
{
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
