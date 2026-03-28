function renderStatusChart(pending, approved, completed) {

    const ctx = document.getElementById("statusChart");

    if (!ctx) return;

    new Chart(ctx, {
        type: "doughnut",

        data: {
            labels: ["Chưa giải quyết", "Đã phê duyệt", "Đã khám"],

            datasets: [{
                data: [pending, approved, completed],

                backgroundColor: [
                    "#ffc107",
                    "#0d6efd",
                    "#28a745"
                ]
            }]
        },

        options: {
            responsive: true,
            plugins: {
                legend: {
                    position: "bottom"
                }
            }
        }
    });
}


function renderMonthlyChart(labels, data) {

    const ctx = document.getElementById('monthChart');

    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Số lịch hẹn',
                data: data,
                backgroundColor: '#4e73df'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false
        }
    });

}
