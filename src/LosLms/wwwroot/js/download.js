// Triggers a client-side file download from a string built in C#.
// Blob and URL.createObjectURL are browser-only APIs that C# cannot reach, so this is the
// minimum interop the CSV export needs. Called via IJSRuntime.InvokeVoidAsync("downloadFile", ...).
// Binary variant, used for the generated CAM.pdf. The string-based downloadFile below cannot carry
// binary content, so this takes a DotNetStreamReference and reads it as an ArrayBuffer instead.
window.downloadFileFromStream = async (filename, contentType, streamReference) => {
    const arrayBuffer = await streamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

window.downloadFile = (filename, content) => {
    const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};
