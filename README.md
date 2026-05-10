# 🐟 Tile Match — Unity Intern Test 2025

> A tile-matching puzzle game built with Unity, featuring multiple game modes, autoplay AI, and a Time Attack challenge.


---


## ✅ Completed Tasks

### Task 2: Change the Gameplay

#### Core Mechanics
- Tap trên cá ở bàn chơi → cá bay xuống khay chứa (dock) gồm 5 ô bên dưới.
- Cá đã xuống khay **không thể kéo ngược lại** bàn (ở chế độ thường).
- Khi có **3 con cá giống nhau liền kề** trong khay → tự động xóa cả 3.

#### Board Initialization
- Bàn chơi luôn sinh ra số lượng cá **chia hết cho 3**, đảm bảo luôn có thể thắng.

#### Win / Lose Conditions
- **Thắng:** Xóa hết toàn bộ cá trên bàn chơi → hiển thị màn hình Win.
- **Thua:** Khay đầy 5 ô mà không có bộ 3 nào khớp → hiển thị màn hình Lose.

#### Autoplay Mode
- **Nút "Auto Win":** Bot tự động tìm cá phù hợp nhất (ưu tiên loại đã có nhiều nhất trong khay) và nhặt mỗi 0.5 giây cho đến khi thắng.
- **Nút "Auto Lose":** Bot tự động chọn cá **khác loại** với những con trong khay, gây thua nhanh nhất có thể.

---

### Task 3: Improve the Gameplay

#### Initial Board Setup
- Thuật toán sinh bàn chơi đảm bảo **tất cả 7 loại cá** đều xuất hiện trên bàn, mỗi loại ít nhất **1 bộ 3 con**. Các slot còn lại được random tự do rồi xáo trộn bằng **Fisher-Yates Shuffle**.

#### Animations
- **Cá bay xuống khay:** Sử dụng `DOJump` (DOTween) tạo hiệu ứng bay vòng cung mượt mà (0.35s, `Ease.OutQuad`).
- **Xóa 3 con giống nhau:** Sử dụng `DOScale(Vector3.zero)` với `Ease.InBack` — cá co lại rồi biến mất (0.3s).
- **Trả cá về bàn (Time Attack):** Cá bay ngược lại ô cũ bằng `DOJump` kèm `DOKill()` để tránh xung đột animation.

#### Time Attack Mode
- **Nút "Time Attack"** trên màn hình chính để vào chế độ riêng biệt.
- **Không thua khi khay đầy** — người chơi có thể tiếp tục chơi.
- **Tap vào cá dưới khay** để trả cá về đúng vị trí ban đầu trên bàn.
- **Bộ đếm ngược 60 giây** hiển thị trên UI, đổi sang **màu đỏ** khi còn ≤ 10 giây. Hết giờ = Thua.

---

## 🛠️ Technical Implementation Notes

### Architecture & Design Patterns

| Pattern | Mô tả | File liên quan |
|---------|--------|---------------|
| **Observer (Action/Event)** | Dùng `Action<T>` và Event để giao tiếp giữa các thành phần mà không phụ thuộc cứng. Ví dụ: `OnTimeUpdateEvent` để cập nhật UI timer, `StateChangedAction` để thông báo trạng thái game. | `BoardController.cs`, `GameManager.cs` |
| **Single Responsibility (SRP)** | Mỗi class chỉ đảm nhận 1 nhiệm vụ duy nhất (xem bảng bên dưới). | Tất cả |

### Animation — DOTween

Toàn bộ animation được xử lý bằng **DOTween**, không dùng Update loop thủ công:

| Hành động | API sử dụng | Thời gian | Ease |
|-----------|-------------|-----------|------|
| Cá bay từ bàn xuống khay | `DOJump()` | 0.35s | `OutQuad` |
| Xóa 3 cá giống nhau | `DOScale(Vector3.zero)` | 0.3s | `InBack` |
| Cá bay ngược về bàn (Time Attack) | `DOJump()` | 0.35s | `OutQuad` |
| Dồn cá trong khay sau khi xóa | `DOMove()` | 0.2s | Default |

> Sử dụng `DOKill()` trước mỗi Tween mới để tránh xung đột khi người chơi spam click.

### Coroutine Flow Control

Sử dụng Coroutine kết hợp `SetBusy(true/false)` để **khóa input** trong lúc animation đang chạy, đảm bảo không xảy ra lỗi state khi người chơi tương tác quá nhanh.

```
Click cá → SetBusy(true) → DOJump bay xuống → Check match → DOScale xóa → Dồn khay → SetBusy(false)
```

---


## 🎮 How to Play

| Chế độ | Mô tả |
|--------|--------|
| **Normal** | Tap cá trên bàn → bay xuống khay → 3 con giống nhau liền kề = nổ. Xóa hết bàn = Thắng. Khay đầy = Thua. |
| **Auto Win** | Bot tự chơi và thắng. |
| **Auto Lose** | Bot tự chơi và thua. |
| **Time Attack** | Như Normal nhưng: không thua khi khay đầy, có thể tap cá dưới khay để trả về bàn, phải xóa hết bàn trong 60 giây. |

---

## 🔧 Requirements

- **Unity Version:** 2021.3+ (LTS)
- **Dependencies:** [DOTween](http://dotween.demigiant.com/) (đã có sẵn trong project)

---

