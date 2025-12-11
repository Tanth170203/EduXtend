// Pagination helper
function goToPage(pageNumber) {
    const url = new URL(window.location.href);
    url.searchParams.set('page', pageNumber);
    
    // Navigate to the new page
    window.location.href = url.toString();
}
