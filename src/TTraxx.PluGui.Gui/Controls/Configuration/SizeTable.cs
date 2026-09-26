namespace TTraxx.PluGui.Gui;

/// <summary>
/// One value per <see cref="ControlSizes"/> tier - how a style keeps everything that varies with a
/// control's size (its layout box, a knob's radius and stroke widths, ...) in one place.
///
/// Complete by construction: every tier is <c>required</c>, so a table that leaves one out doesn't
/// compile, and a lookup can't miss at runtime whichever size a control is given. A control whose
/// look doesn't depend on its tier says so with <see cref="Uniform"/> rather than leaving tiers out.
///
/// A record, so replacing one tier of an existing table is a <c>with</c> expression:
/// <c>KnobStyle.Default.Sizes with { XL = ... }</c>.
/// </summary>
public sealed record SizeTable<T>
{
    public required T S { get; init; }
    public required T M { get; init; }
    public required T L { get; init; }
    public required T XL { get; init; }

    /// <summary>The value for <paramref name="size"/>.</summary>
    public T this[ControlSizes size] => size switch
    {
        ControlSizes.S => S,
        ControlSizes.M => M,
        ControlSizes.L => L,
        ControlSizes.XL => XL,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
    };

    /// <summary>The same value at every tier - for a control whose size doesn't follow <see cref="ControlSizes"/>.</summary>
    public static SizeTable<T> Uniform(T value) => new() { S = value, M = value, L = value, XL = value };
}
