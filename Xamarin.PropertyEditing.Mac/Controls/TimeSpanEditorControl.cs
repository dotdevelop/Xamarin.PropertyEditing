using System;
using System.Collections;
using AppKit;
using Xamarin.PropertyEditing.Mac.Resources;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Mac
{
	internal class TimeSpanEditorControl : PropertyEditorControl<PropertyViewModel<TimeSpan>>
	{
		private readonly TimeSpanTextField editor;

		public TimeSpanEditorControl (IHostResourceProvider hostResource)
			: base (hostResource)
		{
			this.editor = new TimeSpanTextField {
				Font = NSFont.FromFontName (DefaultFontName, DefaultFontSize),
			};

			// update the value on keypress
			editor.Changed += (sender, e) => ViewModel.Value = TimeSpan.Parse (editor.StringValue);

			AddSubview (this.editor);

			this.AddConstraints (new[] {
				NSLayoutConstraint.Create (this.editor, NSLayoutAttribute.Top, NSLayoutRelation.Equal, this,  NSLayoutAttribute.Top, 1f, 1f),
				NSLayoutConstraint.Create (this.editor, NSLayoutAttribute.Width, NSLayoutRelation.Equal, this,  NSLayoutAttribute.Width, 1f, -34f),
				NSLayoutConstraint.Create (this.editor, NSLayoutAttribute.Height, NSLayoutRelation.Equal, 1f, DefaultControlHeight - 3),
			});
		}

		public override NSView FirstKeyView => this.editor;
		public override NSView LastKeyView => this.editor;

		protected override void UpdateValue ()
		{
			this.editor.StringValue = ViewModel.Value.ToString ();
		}

		protected override void HandleErrorsChanged (object sender, System.ComponentModel.DataErrorsChangedEventArgs e)
		{
			UpdateErrorsDisplayed (ViewModel.GetErrors (ViewModel.Property.Name));
		}

		protected override void UpdateErrorsDisplayed (IEnumerable errors)
		{
			if (ViewModel.HasErrors) {
				SetErrors (errors);
			} else {
				SetErrors (null);
				SetEnabled ();
			}
		}

		protected override void SetEnabled ()
		{
			this.editor.Editable = ViewModel.Property.CanWrite;
		}

		protected override void UpdateAccessibilityValues ()
		{
			this.editor.AccessibilityTitle = string.Format (LocalizationResources.AccessibilityTimeSpan, ViewModel.Property.Name);
		}
	}
}