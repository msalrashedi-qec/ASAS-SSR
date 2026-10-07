
window.schoolDashboardCharts = (function () {
    let charts = [];
    function destroy() {
        charts.forEach(chart => chart.destroy());
        charts = [];
    }
    async function render(data) {
        destroy();
        const format = value => Number(value).toLocaleString("en-US", { maximumFractionDigits: 0 });
        const renders = [];
        for (const [key, type] of [["status", "bar"], ["collection", "area"], ["activity", "area"]]) {
            const element = document.getElementById("dashboard-" + key);
            if (!element) continue;
            const values = data[key];
            const chart = new ApexCharts(element, {
                chart: { type, height: 340, fontFamily: getComputedStyle(document.body).fontFamily,
                    toolbar: { show: false }, zoom: { enabled: false } },
                series: values.series,
                colors: values.colors,
                xaxis: { categories: values.categories },
                yaxis: { min: key === "activity" ? undefined : 0, forceNiceScale: true,
                    labels: { formatter: format } },
                dataLabels: { enabled: type === "bar", formatter: format },
                stroke: { curve: "straight", width: type === "area" ? 3 : 0 },
                fill: type === "area"
                    ? { type: "gradient", gradient: { opacityFrom: 0.35, opacityTo: 0.05 } }
                    : { type: "solid" },
                markers: { size: type === "area" ? 3 : 0 },
                legend: { position: "bottom" },
                tooltip: { shared: true, intersect: false,
                    y: { formatter: value => format(value) + (key === "collection" ? " ر.س" : "") } },
                noData: { text: "لا توجد بيانات للعرض" }
            });
            charts.push(chart);
            renders.push(chart.render());
        }
        await Promise.all(renders);
    }
    return { render, destroy };
})();

window.dashboardCharts = (function () {
    let trendChart;
    let statusChart;

    function destroy() {
        if (trendChart) { trendChart.destroy(); trendChart = null; }
        if (statusChart) { statusChart.destroy(); statusChart = null; }
    }

    function render(trendSelector, statusSelector, months, caseData, consultationData, statusLabels, statusValues, statusColors, casesLabel, consultationsLabel, isRtl) {
        destroy();
        const trendElement = document.querySelector(trendSelector);
        const statusElement = document.querySelector(statusSelector);
        if (!trendElement || !statusElement || typeof ApexCharts === "undefined") return;
        const styles = getComputedStyle(document.documentElement);
        const font = getComputedStyle(document.body).fontFamily;
        const muted = styles.getPropertyValue("--text-muted").trim() || "#94a3b8";
        const border = styles.getPropertyValue("--border").trim() || "#e2e8f0";

        trendChart = new ApexCharts(trendElement, {
            series: [{ name: casesLabel, data: caseData }, { name: consultationsLabel, data: consultationData }],
            chart: { type: "area", height: 270, fontFamily: font, toolbar: { show: false }, zoom: { enabled: false } },
            colors: ["#658ca7", "#00aedb"], dataLabels: { enabled: false }, stroke: { curve: "smooth", width: 3 },
            fill: { type: "gradient", gradient: { shadeIntensity: 1, opacityFrom: .26, opacityTo: .02, stops: [0, 95] } },
            markers: { size: 0, hover: { size: 5 } }, grid: { borderColor: border, strokeDashArray: 5 },
            xaxis: { categories: months, labels: { style: { colors: muted, fontSize: "11px" } }, axisBorder: { show: false }, axisTicks: { show: false } },
            yaxis: { min: 0, forceNiceScale: true, labels: { formatter: value => Math.round(value), style: { colors: muted, fontSize: "11px" } } },
            legend: { position: "top", horizontalAlign: isRtl ? "right" : "left", fontSize: "11px" }, tooltip: { shared: true, intersect: false }
        });
        statusChart = new ApexCharts(statusElement, {
            series: statusValues, labels: statusLabels, chart: { type: "donut", height: 235, fontFamily: font }, colors: statusColors,
            stroke: { width: 3, colors: ["transparent"] }, dataLabels: { enabled: false },
            plotOptions: { pie: { donut: { size: "72%", labels: { show: true, name: { show: true, color: muted }, value: { show: true, fontSize: "24px", fontWeight: 700 }, total: { show: true, label: casesLabel, color: muted, formatter: w => w.globals.seriesTotals.reduce((a, b) => a + b, 0) } } } } },
            legend: { show: true, position: "bottom", fontSize: "10px", itemMargin: { horizontal: 7, vertical: 4 } }, noData: { text: "—" }
        });
        trendChart.render(); statusChart.render();
    }
    return { render: render, destroy: destroy };
})();

function getCompalted(title, dataList) {

    $(function () {
        "use strict";
        var nightingaleChart = echarts.init(
            document.getElementById("nightingale-chart")
        );
        var option = {
            //title: {
            //    text: "Employee's salary review",
            //    subtext: "Senior front end developer",
            //    x: "center",
            //},

            // Add tooltip
            tooltip: {
                trigger: "item",
                formatter: "{a} <br/>{b}: {c} ({d}%)"
            },

            // Add legend
            legend: {
                x: "left",
                y: "top",
                orient: "vertical",
                show: false,
                data: [
                    "January",
                    "February",
                    "March",
                    "April",
                    "May",
                    "June",
                    "July",
                    "August",
                    "September",
                    "October",
                    "November",
                    "December"
                ]
            },

            color: [
                "#ffbc34",
                "#00acc1",
                "#212529",
                "#f62d51",
                "#1e88e5",
                "#FFC400",
                "#006064",
                "#FF1744",
                "#1565C0",
                "#FFC400",
                "#64FFDA",
                "#607D8B"
            ],

            // Enable drag recalculate
            calculable: true,
            // Add series
            series: [
                {
                    name: title,
                    type: "pie",
                    radius: ["15%", "73%"],
                    center: ["50%", "57%"],
                    roseType: "area",
                    // Funnel
                    width: "100%",
                    height: "100%",
                    //x: "30%",
                    //y: "17.5%",
                    max: 450,
                    sort: "ascending",
                    data: dataList

                },
            ],
        };
        nightingaleChart.setOption(option);
    });

}
function getLastTwoMonths(list) {

    $(function () {

        var option_Newsletter_Campaign = {
            series: list,
            chart: {
                fontFamily: "Poppins,sans-serif",
                height: 300,
                type: "area",
                toolbar: {
                    show: false
                }
            },
            fill: {
                type: "gradient",
                gradient: {
                    shadeIntensity: 1,
                    opacityFrom: 0.5,
                    opacityTo: 0.5,
                    stops: [0, 90, 100]
                },
                colors: ["#1e88e5", "#fc4b6c"]
            },
            grid: {
                show: true,
                strokeDashArray: 8,
                borderColor: "rgba(0,0,0,.1)",
                xaxis: {
                    lines: {
                        show: true
                    }
                },
                yaxis: {
                    lines: {
                        show: true
                    }
                }
            },
            colors: ["#1e88e5", "#fc4b6c"],
            dataLabels:
            {
                enabled: false
            },
            stroke: {
                curve: "smooth",
                width: 4
            },
            markers: {
                size: 3,
                strokeColors: "transparent"
            },
            xaxis: {
                categories: ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31"],
                labels: {
                    style: {
                        colors: "black"
                    }
                }

            },
            yaxis: {
                labels: {
                    style: {
                        colors: "black"
                    },
                    formatter: Math.floor
                }
            },
            tooltip: {
                theme: "dark",
                x: {
                    format: '{n2}%'
                }

            },
            legend: {
                show: false
            }
        };
        var chart_area_spline = new ApexCharts(
            document.querySelector("#LastTowMonth-id"),
            option_Newsletter_Campaign
        );
        chart_area_spline.render();
    });
}
function areaChart(selectorName, labelsList, valuesList, colorsList) {
    $(function () {
        "use strict";
        var option_Newsletter_Campaign = {
            series: labelsList,
            //[
            //    { "name": "Mohammed", data: [10, 15, 20, 25] },
            //    { "name": "Mohammed", data: [10, 15, 20, 25] },
            //    { "name": "Munawer", data: [100, 15, 50, 25] },
            //],
            chart: {
                fontFamily: "Poppins,sans-serif",
                height: 260,
                type: "area",
                toolbar: {
                    show: false
                },
            },
            fill: {
                type: "gradient",
                gradient: {
                    shadeIntensity: 1,
                    opacityFrom: 0.5,
                    opacityTo: 0.5,
                    stops: [0, 90, 100]
                },
                colors: colorsList,/*["#21c1d6", "#00ff70", "#343a40", "#7460ee", "#6c757d", "#fc4b6c", "#ffb22b", "#1e88e5"],*/
            },
            grid: {
                show: true,
                strokeDashArray: 8,
                borderColor: "rgba(0,0,0,.1)",
                xaxis: {
                    lines: {
                        show: true
                    }
                },
                yaxis: {
                    lines: {
                        show: true
                    }
                }
            },
            colors: colorsList,//["#21c1d6", "#00ff70", "#343a40", "#7460ee", "#6c757d", "#fc4b6c", "#ffb22b", "#1e88e5"],
            dataLabels: {
                enabled: false
            },
            stroke: {
                curve: "smooth",
                width: 2
            },
            markers: {
                size: 3,
                strokeColors: "transparent",
            },
            xaxis: {
                categories: ["1", "2"],
                labels: {
                    style: {
                        colors: "#a1aab2"
                    }
                }
            },
            yaxis: {
                labels: {
                    style: {
                        colors: "#a1aab2"
                    }
                }
            },
            tooltip: {
                theme: "dark"
            },
            legend: {
                show: false
            }
        };

        var chart_area_spline = new ApexCharts(
            document.querySelector(selectorName),
            option_Newsletter_Campaign
        );
        chart_area_spline.render();
    });
}
function getResponeRate(clockWise, titleName, titleRegistrationclosed, titlecompalintReplied, titleenquiriesReplied, titletiketsReplied, registerValue, complaintValue, enquiriesValue, ticketValue) {
    $(function () {
        "use strict";

        var poleChart = echarts.init(document.getElementById("pole-chart"));
        var posisionleft = 0;
        var posisionright = 0;
        if (clockWise) {
            posisionright = 0;
            posisionleft = 30;
        }
        else {
            posisionright = document.getElementById("pole-chart").offsetWidth / 2;
            posisionleft = 0;
        }

        var dataStyle = {
            normal: {
                label: { show: false },
                labelLine: { show: false }
            }
        };
        var placeHolderStyle = {
            normal: {
                color: "#eceff1",
                label: { show: false },
                labelLine: { show: false }
            },
            emphasis: {
                color: "rgba(0,0,0,0)"
            }
        };
        var option = {
            title: {
                text: titleName,
                show: false,
                x: "center",
                y: "center",
                itemGap: 10,
                textStyle: {
                    color: "rgba(30,144,255,0.8)",
                    fontSize: 19,
                    fontWeight: "500"
                }
            },
            // Add tooltip
            tooltip: {
                show: true,
                formatter: "{a} <br/>{b}: {c} ({d}%)"
            },

            // Add legend
            legend: {
                orient: "vertical",
                x: posisionright,
                y: posisionleft,
                itemGap: 15,
                data: [
                    titleRegistrationclosed,
                    titlecompalintReplied,
                    titleenquiriesReplied,
                    titletiketsReplied
                ]
            },
            // Add custom colors
            color: ["#1e88e5", "#fc4b6c", "#ffb22b", "#7460ee"],

            // Add series
            series: [
                {
                    name: "طلبات التسجيل",
                    type: "pie",
                    clockWise: clockWise,
                    radius: ["75%", "90%"],
                    itemStyle: dataStyle,
                    data: [
                        {
                            value: registerValue,
                            name: titleRegistrationclosed,
                        },
                        {
                            value: 100 - registerValue,
                            name: "invisible",
                            itemStyle: placeHolderStyle
                        }
                    ]
                },
                {
                    name: "الشكاوى",
                    type: "pie",
                    clockWise: clockWise,
                    radius: ["60%", "75%"],
                    itemStyle: dataStyle,
                    data: [
                        {
                            value: complaintValue,
                            name: titlecompalintReplied
                        },
                        {
                            value: 100 - complaintValue,
                            name: "invisible",
                            itemStyle: placeHolderStyle
                        }
                    ]
                },
                {
                    name: "الاستفسارات",
                    type: "pie",
                    clockWise: clockWise,
                    radius: ["45%", "60%"],
                    itemStyle: dataStyle,
                    data: [
                        {
                            value: enquiriesValue,
                            name: titleenquiriesReplied
                        },
                        {
                            value: 100 - enquiriesValue,
                            name: "invisible",
                            itemStyle: placeHolderStyle
                        }
                    ]
                },
                {
                    name: "الطلبات",
                    type: "pie",
                    clockWise: clockWise,
                    radius: ["30%", "45%"],
                    itemStyle: dataStyle,
                    data: [
                        {
                            value: ticketValue,
                            name: titletiketsReplied
                        },
                        {
                            value: 100 - ticketValue,
                            name: "invisible",
                            itemStyle: placeHolderStyle
                        }
                    ]
                }
            ]
        };

        poleChart.setOption(option);
    });
}
function donutChart(selectorName, labelsList, valuesList, colorsList) {
    $(function () {
        var option_Our_prestanages = {
            series: valuesList,
            labels: labelsList,
            chart: {
                type: "pie",
                height: 300,
                fontFamily: "Poppins,sans-serif",
            },
            dataLabels: {
                enabled: false
            },
            stroke: {
                width: 0
            },
            plotOptions: {
                pie: {
                    expandOnClick: true,
                    pie: {
                        size: "83",
                        labels: {
                            show: true,
                            name: {
                                show: true,
                                offsetY: 7
                            },
                            value: {
                                show: true
                            },
                            total: {
                                show: false,
                                color: "#a1aab2",
                                fontSize: "13px",
                                label: "Prestanage"
                            }
                        }
                    }
                }
            },
            colors: colorsList,
            tooltip: {
                show: true,
                fillSeriesColor: false
            },
            legend: {
                show: true,
                formatter: function (seriesName, opts) {
                    return seriesName + ":  " + opts.w.globals.series[opts.seriesIndex];
                },
            },
            responsive: [
                {
                    breakpoint: 480,
                    options: {
                        chart: {
                            width: 200
                        }
                    }
                }
            ]
        };

        var chart_pie_donut = new ApexCharts(
            document.querySelector(selectorName),
            option_Our_prestanages
        );
        chart_pie_donut.render();

    });
}
function getHeatMap(datat, serviceLink, serviceName) {
    var schoolData = datat;
    $(function () {
        HeatMap = new GMaps({
            div: "#HeatMap",
            lat: 24.774265,
            lng: 46.738586,
            zoom: 10
        });

        schoolData.forEach(item => {
            HeatMap.addMarker({
                lat: item.Latitude,
                lng: item.Longitude,
                title: item.schoolname,
                icon: pinSymbol(item.color),
                infoWindow: {
                    content: "<p>" + item.schoolname + "</p>"
                        + "<table class='table'><tr><td>" + serviceName + "</td><td><a href='" + serviceLink + ".aspx?s=" + item.SchoolId + "'>" + item.RowsCount + "</a></td></tr></table>"

                }
            });
        });
    });
}

function getGlobleHeatMap(datat) {
    var schoolData = datat;
    $(function () {
        HeatMap = new GMaps({
            div: "#HeatMap",
            lat: 24.774265,
            lng: 46.738586,
            zoom: 10

        });

        schoolData.forEach(item => {
            HeatMap.addMarker({
                lat: item.Latitude,
                lng: item.Longitude,
                title: item.schoolname,
                icon: pinSymbol(item.color),
                infoWindow: {
                    content: "<p>" + item.schoolname + "</p>"
                        + "<table class='table'><tr style='border:2px;font-size:15px'><td>" + item.serviceName + "</td><td>" + item.RowsCount + "</td></tr>"
                        + "<tr><td>" + item.register + "</td><td><a href='ResgistrationsDashboard.aspx?s=" + item.SchoolId + "'>" + item.RegisterRowsCount + "</a></td></tr>"
                        + "<tr><td>" + item.Complaint + "</td><td><a href='ComplaintsDashboard.aspx?s=" + item.SchoolId + "'>" + item.ComplaintRowsCount + "</a></td></tr>"
                        + "<tr><td>" + item.Enquiry + "</td><td><a href='EnquiriesDashboard.aspx?s=" + item.SchoolId + "'>" + item.EnquiryRowsCount + "</a></td></tr>"
                        + "<tr><td>" + item.tickets + "</td><td><a href='TicketsDashboard.aspx?s=" + item.SchoolId + "'>" + item.TicketRowsCount + "</a></td></tr>"
                        + "</table> "

                }
            });
        });
    });
}
function pinSymbol(color) {
    return {
        path: 'M 0,0 C -2,-20 -10,-22 -10,-30 A 10,10 0 1,1 10,-30 C 10,-22 2,-20 0,0 z',
        fillColor: color,
        fillOpacity: 1,
        strokeColor: '#000',
        strokeWeight: 2,
        scale: 1
    };
}

