using NPlug;
using TTraxx.PluGui.Harness.NPlug.Core.Extensions;
using TTraxx.PluGui.Harness.NPlug.Core.Handlers;
using TTraxx.PluGui.Harness.NPlug.Core.Interfaces;

namespace TTraxx.PluGui.Harness.NPlug.Core;

public abstract class AbstractHarnessPluginBase<TController, TModel, TView>(AudioPluginViewPlatform platform) : IHarnessPlugin
    where TController : AudioController<TModel>, new()
    where TModel : AudioProcessorModel, new()
    where TView : IAudioPluginView
{
    public abstract string DisplayName { get; }
    public virtual AudioPluginViewPlatform Platform => platform;

    public virtual IAudioPluginView Create()
    {
        TController controller = new();
        ((IAudioController)controller).SetControllerHandler(new NoOpComponentHandler());
        controller.ReassertParameterDefaults();
        return CreateView(controller);
    }

    /// <summary>
    /// Base method to create the view for the plugin. This method is called after the controller has been created and initialized.
    /// Concrete implementation: This method should create and return an instance of the view associated with the plugin, using the provided controller.
    /// (return new <ViewName>(controller, controller.Model))
    /// </summary>
    /// <param name="controller">The controller for which to create a view.</param>
    /// <returns>An instance of the view associated with the plugin.</returns>
    protected abstract TView CreateView(TController controller);
}
