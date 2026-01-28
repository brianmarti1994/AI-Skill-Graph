window.cvCharts = {
    _charts: {},

    renderBar: function (canvasId, labels, data) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
        }

        this._charts[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{ label: 'Years', data: data }]
            },
            options: {
                responsive: true,
                scales: { y: { beginAtZero: true } }
            }
        });
    }
};
