public class NullSafeComparer : IComparer
{
    private readonly string _propertyPath;
    private readonly ListSortDirection _direction;

    public NullSafeComparer(string propertyPath, ListSortDirection direction)
    {
        _propertyPath = propertyPath;
        _direction = direction;
    }

    public int Compare(object x, object y)
    {
        var xVal = GetPropertyValue(x, _propertyPath)?.ToString() ?? "";
        var yVal = GetPropertyValue(y, _propertyPath)?.ToString() ?? "";

        var result = string.Compare(xVal, yVal, StringComparison.CurrentCultureIgnoreCase);

        return _direction == ListSortDirection.Ascending ? result : -result;
    }

    private object GetPropertyValue(object obj, string path)
    {
        foreach (var part in path.Split('.'))
        {
            if (obj == null) return null;
            var prop = obj.GetType().GetProperty(part);
            obj = prop?.GetValue(obj);
        }
        return obj;
    }
}
