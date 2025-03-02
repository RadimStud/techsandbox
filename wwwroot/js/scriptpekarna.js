document.addEventListener("DOMContentLoaded", function () {
    const detailModal = document.getElementById("detailModal");
    const closeModal = document.getElementById("closeModal");

    closeModal.addEventListener("click", function () {
        detailModal.style.display = "none";
    });

    window.addEventListener("click", function (event) {
        if (event.target === detailModal) {
            detailModal.style.display = "none";
        }
    });
});

function ukazDetail(kolac) {
    const modalTitle = document.getElementById("modalTitle");
    const modalText = document.getElementById("modalText");

    const recepty = {
        jahodovy: {
            title: "Jahodový koláč",
            text: "Tento koláč je plný čerstvých jahod a křupavého těsta."
        },
        malinovy: {
            title: "Malinový koláč",
            text: "Lahodný malinový koláč s jemným těstem a sladkou náplní."
        },
        boruvkovy: {
            title: "Borůvkový koláč",
            text: "Klasický borůvkový koláč s bohatou ovocnou chutí."
        }
    };

    if (recepty[kolac]) {
        modalTitle.innerText = recepty[kolac].title;
        modalText.innerText = recepty[kolac].text;
        document.getElementById("detailModal").style.display = "flex";
    }
}
