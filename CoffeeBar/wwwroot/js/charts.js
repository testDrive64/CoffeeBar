// Thin bridge between Blazor (Shared/ChartCanvas.razor) and Chart.js 4.
// The chart configuration is built in C#; this file only creates, replaces and destroys charts.
window.coffeeCharts = {
    render: function (canvas, config) {
        if (!canvas) {
            return;
        }
        var existing = Chart.getChart(canvas);
        if (existing) {
            existing.destroy();
        }
        // Same font as the rest of the app
        Chart.defaults.font.family = getComputedStyle(document.body).fontFamily;
        new Chart(canvas, config);
    },

    destroy: function (canvas) {
        if (!canvas) {
            return;
        }
        var existing = Chart.getChart(canvas);
        if (existing) {
            existing.destroy();
        }
    }
};
