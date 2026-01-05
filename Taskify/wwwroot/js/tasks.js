// Hàm gọi API và render lại Dropdown/List
function loadMemberSuggestions(teamId, taskTitle, taskDesc, targetElementId, isDropdown = true) {
    if (!teamId) return;

    // Hiển thị loading
    var $target = $(targetElementId);
    $target.prop('disabled', true);

    $.ajax({
        url: '/api/TaskHelper/suggestions',
        type: 'GET',
        data: { teamId: teamId, title: taskTitle, desc: taskDesc },
        success: function (data) {
            $target.empty(); // Xóa cũ

            if (isDropdown) {
                // CASE 1: Dùng cho thẻ <select> (Create Task)
                $target.append('<option value="">--Choose member assign --</option>');

                $.each(data, function (i, item) {
                    // Hiển thị điểm ngay trong text: "Tên (85% Match)"
                    var text = `${item.name} (${item.score}% fit)`;
                    var option = `<option value="${item.id}" data-score="${item.score}">${text}</option>`;
                    $target.append(option);
                });
            } else {
                // CASE 2: Dùng cho List <ul> (Add Member Modal)
                $.each(data, function (i, item) {
                    var colorClass = item.score >= 80 ? 'bg-success' : (item.score >= 50 ? 'bg-warning' : 'bg-danger');

                    var html = `
                        <li class="list-group-item d-flex justify-content-between align-items-center p-3">
                            <div class="d-flex align-items-center">
                                <img src="${item.avatar}" class="rounded-circle me-3" width="40" height="40">
                                <div>
                                    <h6 class="mb-0">${item.name}</h6>
                                    <small class="text-muted">Độ phù hợp:</small>
                                    <div class="progress" style="height: 5px; width: 100px;">
                                        <div class="progress-bar ${colorClass}" role="progressbar" style="width: ${item.score}%"></div>
                                    </div>
                                </div>
                            </div>
                            <button class="btn btn-sm btn-outline-primary btn-add-member" data-user-id="${item.id}">
                                <i class="fas fa-plus"></i> Choose
                            </button>
                        </li>
                    `;
                    $target.append(html);
                });
            }
            $target.prop('disabled', false);
        },
        error: function () {
            console.error("Member suggestion loading error");
            $target.prop('disabled', false);
        }
    });
}