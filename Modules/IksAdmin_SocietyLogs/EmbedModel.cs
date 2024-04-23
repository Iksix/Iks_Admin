using Discord.Webhook;

namespace IksAdmin_SocietyLogs;

public class EmbedModel
{
    public string Title { get; set; }
    public string Description { get; set; }
    public ColorModel Color { get; set; }
    public FieldModel[] Fields { get; set; }

    public EmbedModel(string title, string description, ColorModel color, FieldModel[] fields)
    {
        Title = title;
        Description = description;
        Color = color;
        Fields = fields;
    }

    public FieldModel[] GetFields()
    {
        List<FieldModel> fields = new();
        foreach (var f in Fields)
        {
            fields.Add(new FieldModel(f.Name, f.Value, f.InLine));
        }

        return fields.ToArray();
    }
    public string GetTitle()
    {
        string title = Title.ToString();

        return title;
    }
    public string GetDescription()
    {
        string title = Description.ToString();

        return title;
    }
}

public class FieldModel
{
    public string Name { get; set; }
    public string Value { get; set; }
    public bool InLine { get; set; }

    public FieldModel(string name, string value, bool inLine = false)
    {
        Name = name;
        Value = value;
        InLine = inLine;
    }
}
public class ColorModel
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }

    public ColorModel(int r, int g, int b)
    {
        R = r;
        G = g;
        B = b;
    }
}
