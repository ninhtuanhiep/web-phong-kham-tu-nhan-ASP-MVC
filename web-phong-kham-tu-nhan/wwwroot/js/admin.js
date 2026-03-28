function removeVietnameseTones(str) {

    return str
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replace(/đ/g, "d")
        .replace(/Đ/g, "D");

}
function searchTable(inputId, tableId) {
    const searchInput = document.getElementById(inputId);

    if (!searchInput) return;

    searchInput.addEventListener("keyup", function () {

        let filter = removeVietnameseTones(this.value.toLowerCase());
        let rows = document.querySelectorAll(`#${tableId} tbody tr`);

    rows.forEach(function (row) {

        let text = removeVietnameseTones(row.innerText.toLowerCase());

    if (text.includes(filter)) {
        row.style.display = "";
        } else {
        row.style.display = "none";
        }

    });

  });
}

function XacNhan_Xoa() {
    const deleteButtons = document.querySelectorAll(".btn-delete");

    deleteButtons.forEach(function (btn) {
        btn.addEventListener("click", function (e) {
            const comfirmDelete = confirm("Bạn có muốn xóa không?");
            if (!comfirmDelete) {
                e.preventDefault();
            }
        });
    });
}

function AnThongBao_XacNhan() {
    const toast = document.getElementById("toastMessage");

    if (!toast) return;

    setTimeout(function () {

        toast.classList.add("toast-hide");

        setTimeout(function () {
            toast.remove();
        }, 500);

    }, 2000);
}



document.addEventListener("DOMContentLoaded", function () {
    searchTable("searchInput","specialtyTable");
    searchTable("searchInput","doctorTable");
    searchTable("searchInput","pateintTable");
    searchTable("searchInput","UserTable");
    XacNhan_Xoa();
    AnThongBao_XacNhan();
});