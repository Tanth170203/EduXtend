using System.Collections.Generic;

namespace BusinessObject.DTOs;

public class PagedResult<T>
{
	public List<T> Items { get; set; } = new();
	public int PageNumber { get; set; }
	public int PageSize { get; set; }
	public int TotalItems { get; set; }
	public int TotalPages { get; set; }
	public bool HasPrevious => PageNumber > 1;
	public bool HasNext => PageNumber < TotalPages;
}


