using System.CommandLine.Binding;

namespace FluentCommandLine;

public abstract class AbstractSettingsBinder<TSettings> : BinderBase<TSettings>
{
	public List<IValueDescriptor> ValueDescriptors { get; set; } = [];

	public TSettings GetSettings(BindingContext bindingContext)
	{
		return GetBoundValue(bindingContext);
	}
}