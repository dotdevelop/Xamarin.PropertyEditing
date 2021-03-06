using System;

namespace Xamarin.PropertyEditing
{
	public enum ValueSource
	{
		/// <summary>
		/// Value is the property's default value, which is not the same as the default value of the property type.
		/// </summary>
		Default = 0,
		Local = 1,
		Binding = 2,
		Resource = 3,
		Style = 4,
		Inherited = 5,
		DefaultStyle = 6,

		/// <summary>
		/// The property's value comes from multiple sources.
		/// </summary>
		Unknown = 7,

		/// <summary>
		/// The property's value is unset but its unset value isn't known.
		/// </summary>
		Unset = 8,
	}

	[Flags]
	public enum ValueSources
	{
		/// <summary>
		/// Property can indicate its unset and not simply at the same value as its value type.
		/// </summary>
		/// <remarks>
		/// Platforms like WPF can make a distinction between whether the value has no value set by any means,
		/// or if a value is set locally (even if it's the same as the default for the value type). Only platforms
		/// that can support this distinction should enable this source.
		/// </remarks>
		Default = 1,
		Local = 2,
		Binding = 4,
		Resource = 8,
		Style = 16
	}
}