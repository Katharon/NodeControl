namespace NodeControl.Domain.Templates;

public enum TemplateType
{
    GenericText = 1,
    Jinja2 = 2,
    AnsibleVars = 3,
    ShellScript = 4,
    ConfigFile = 5
}
