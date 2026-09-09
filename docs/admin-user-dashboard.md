# Admin user dashboard

Tất cả endpoint dưới `/api/admin/dashboard` yêu cầu JWT role `Admin`.
Response thành công trả DTO trực tiếp (HTTP 200), validation sai trả HTTP 400.
Chưa đăng nhập trả 401; role khác trả 403.

## Tổng quan

`GET /api/admin/dashboard/users/overview?role=Personal`

`role` tùy chọn: `Personal`, `Business`, `Moderator`, `Admin`.
Không truyền role: tính tất cả tài khoản, bao gồm nội bộ và Deleted.

- `totalAccounts`: tổng tài khoản thuộc phạm vi role.
- `emailVerifiedAccounts`: số tài khoản đã xác thực email.
- `byStatus`: số lượng Pending/Active/Suspended/Deleted, bao gồm nhóm có số lượng 0.
- `byRole`: số lượng theo role, cùng phạm vi lọc; role ngoài phạm vi có số lượng 0.
- `generatedAtUtc`: thời điểm tạo response.

`Active` là trạng thái tài khoản, không phải số người online.
Các số liệu này là trạng thái hiện tại, không phải snapshot lịch sử.

## Danh sách theo trạng thái

`GET /api/admin/dashboard/users?status=Suspended&role=Personal&keyword=tram&pageNumber=1&pageSize=10`

Mọi bộ lọc đều tùy chọn. `keyword` tìm trong username/email/điện thoại, tối đa 200 ký tự.
`pageNumber`: 1–1.000.000; `pageSize`: 1–100, mặc định 10.
Danh sách sắp xếp CreatedAt giảm dần, sau đó UserId để ổn định thứ tự.
Trả `items`, `totalCount`, `pageNumber`, `pageSize`, `totalPages`, `hasPreviousPage`, `hasNextPage`.
`totalCount` áp dụng tất cả bộ lọc. Không có bản ghi: danh sách rỗng, tổng bằng 0.
Không trả password/token.

## Xu hướng và dự đoán đăng ký

`GET /api/admin/dashboard/users/registration-trend?days=30&forecastDays=7&role=Personal`

- `days`: 7–90, mặc định 30. So sánh hai kỳ liên tiếp cùng độ dài.
- `forecastDays`: 1–30, mặc định 7.
- Múi giờ Việt Nam UTC+7. Loại ngày hôm nay chưa hoàn tất khỏi dữ liệu huấn luyện/so sánh.
- `dailyRegistrations`: đủ 2 × days ngày, ngày không có đăng ký trả 0.
- `previousPeriodRegistrations`, `currentPeriodRegistrations`: số đăng ký trong từng kỳ.
- `registrationChange`: kỳ hiện tại trừ kỳ trước.
- `growthPercent`: (hiện tại − trước) / trước × 100; null khi kỳ trước bằng 0.
- `direction`: Increasing/Decreasing/Stable theo chênh lệch số đăng ký.
- `averageDailyRegistrations`: số đăng ký kỳ hiện tại / days.
- `forecast.estimatedRegistrations`: trung bình chưa làm tròn × forecastDays, làm tròn kết quả 2 chữ số.
- Khoảng dự đoán bắt đầu hôm nay, kết thúc trước `forecast.endDateExclusive`.
- `forecast.method = TrailingDailyAverage`, `isEstimate = true`; không có khoảng tin cậy thống kê.

Ví dụ days=7: kỳ trước có 7 đăng ký, kỳ hiện tại có 14 thì tăng 100%,
trung bình 2/ngày, dự đoán 3 ngày được 6 đăng ký.

Đây là baseline từ tốc độ đăng ký gần đây, không ngoại suy tăng trưởng, mùa vụ hoặc chiến dịch.
Không có dữ liệu vẫn trả dự đoán 0 và observedRegistrations=0; không có nghĩa chắc chắn không ai đăng ký.
Hệ thống chưa lưu ngày bắt đầu quan sát nên ngày thiếu bản ghi được giả định có 0 đăng ký.
Không lọc theo status để tránh mất các lượt đăng ký sau đó bị khóa/xóa mềm.
Role được lọc theo role hiện tại; chưa có lịch sử đổi role.
User.CreatedAt chỉ là proxy cho đăng ký: tài khoản import/tạo nội bộ cũng được tính nếu nằm trong role đã chọn.
Nếu tài khoản bị xóa vật lý, lịch sử đếm từ Users không còn đầy đủ.

## Kiểm tra

```powershell
dotnet build HomeCycle-platform.sln --no-restore
dotnet run --project tests/HomeCycle.Dashboard.Checks
```

Regression runner kiểm tra phép tính, các kỳ rỗng, ranh giới ngày UTC+7, validation,
tổng theo trạng thái và khai báo quyền Admin. Không thay thế kiểm thử HTTP/JWT và PostgreSQL thực tế.
Không cần migration cho phiên bản này. Khi dữ liệu lớn, đánh giá execution plan để thêm index
CreatedAt hoặc (Role, CreatedAt), (Status, CreatedAt) theo nhu cầu sử dụng thực tế.
