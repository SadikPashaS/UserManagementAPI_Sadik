
using System.Collections.Generic;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Skip, int Take);