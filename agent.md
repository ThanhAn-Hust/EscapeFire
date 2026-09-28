# AGENT GUIDE & GIT STRATEGY - DỰ ÁN UNITY VR EDTECH

Tài liệu này hướng dẫn chiến lược quản lý Git, cấu hình `.gitignore` chuẩn Unity VR, và kế hoạch phân chia Commit theo chuẩn **Conventional Commits** cho dự án **VR Emergency Safety Simulator**.

---

## 📌 1. GIẢI ĐÁP CƠ CẤU FOLDER: CÁI NÀO IGNORE - CÁI NÀO KEEP?

### ❌ 1. Thư mục `ProjectSettings/` -> **KHÔNG ĐƯỢC GITIGNORE!** (Bắt buộc phải Commit)
* **Lý do:** Thư mục này chứa toàn bộ cấu hình dự án Unity:
  * Cấu hình **XR Plugin Management** (Meta Quest Loader, Controller mapping).
  * Cấu hình **URP Graphics & Quality Settings**.
  * Cấu hình **Tags, Layers, Input System**.
* **Hậu quả nếu Ignore:** Người khác hoặc Giảng viên clone code về sẽ bị **mất sạch cấu hình VR và vỡ Shader**, không thể chạy được dự án!

### ❓ 2. Thư mục `Assets/Assets_ServerRacks` & 3D Assets -> **KHÔNG GITIGNORE (PHẢI KEEP)**
* **Lý do:** Đây là các 3D Model cốt lõi của bài học (Tủ Server, thiết bị phòng lab). Nếu ignore, khi clone về dự án sẽ bị lỗi `Missing Prefabs / Pink Material` (màn hình tím).
* **Giải pháp tối ưu dung lượng:** Nên bật **Git LFS (Large File Storage)** để quản lý các file dung lượng lớn như `.fbx`, `.obj`, `.png`, `.psd`, `.wav`.

### 🗑️ 3. Thư mục `/Samples` (Nằm trong Assets hoặc Packages) -> **NÊN XÓA BỚT / IGNORE**
* **Lý do:** Thư mục `Samples` thường chứa các cảnh mẫu (Sample Scenes) và Asset thử nghiệm đi kèm của các package tải về, gây nặng dung lượng Git mà không dùng đến trong game thực tế.
* **Hành động:** Xóa các thư mục `Samples` không cần thiết trước khi commit để nhẹ Repo.

---

## 📑 2. MẪU FILE `.gitignore` CHUẨN UNITY DÀNH CHO GIÁM SÁT DỰ ÁN

Tạo file `.gitignore` ở thư mục gốc dự án với nội dung:

```gitignore
# Unity temporary & build folders (BẮT BUỘC IGNORE)
/[L|l]ibrary/
/[T|t]emp/
/[O|o]bj/
/[B|b]uild/
/[B|b]uilds/
/[L|l]ogs/
/[U|u]ser[S|s]ettings/
/[M|m]emoryCaptures/

# IDE files
/.vs/
/.idea/
/*.csproj
/*.sln
/*.unityproj

# OS Generated
.DS_Store
Thumbs.db
```

---

## 🔀 3. KẾ HOẠCH PHÂN CHIA COMMIT THEO CHUẨN CONVENTIONAL COMMITS

Để lịch sử Git chuyên nghiệp (chuẩn đồ án tốt nghiệp / dự án thực tế), hãy thực hiện commit chia nhỏ theo từng bước bên dưới:

```bash
# 1. Commit cấu hình Git cơ bản
git add .gitignore .gitattributes
git commit -m "chore(git): setup unity gitignore and git lfs tracking configuration"

# 2. Commit tài liệu kịch bản & hướng dẫn dự án
git add agent.md game_scenario_script.md
git commit -m "docs(spec): add detailed game scenario script and agent guide"

# 3. Commit cấu hình ProjectSettings & XR/URP Settings
git add ProjectSettings/ Packages/manifest.json Packages/packages-lock.json
git commit -m "feat(config): initialize project settings with URP and XR Interaction Toolkit"

# 4. Commit các 3D Asset môi trường & Tủ Server (Nội thất)
git add Assets/POLYGON_Office/ Assets/Assets_ServerRacks/
git commit -m "feat(assets): import POLYGON office and server rack 3d models"

# 5. Commit các hiệu ứng VFX Lửa & Khói độc
git add Assets/Responsive_Smokes_URP/ Assets/Real_Fire_Smoke/
git commit -m "feat(vfx): integrate URP volumetric smoke and fire particle effects"

# 6. Commit các Plugin AI Mentor (Google Cloud STT & Overtone TTS)
git add Assets/GoogleCloudSpeech/ Assets/OvertoneTTS/
git commit -m "feat(ai): integrate google cloud STT and overtone offline TTS plugins"

# 7. Commit dọn dẹp các file mẫu rác/sample không dùng
git commit -m "refactor(clean): remove unused sample scenes and redundant sample assets"
```

---

## 💡 QUY TẮC ĐẶT MESSAGE COMMIT (QUY CHUẨN)

* `feat:` (Feature) - Thêm tính năng mới, asset mới, hoặc cấu hình mới.
* `docs:` (Documentation) - Cập nhật file hướng dẫn, kịch bản, README.
* `chore:` (Chore) - Cấu hình hệ thống (Gitignore, LFS, cấu hình build).
* `refactor:` (Refactor) - Tối ưu code, dọn dẹp thư mục rác mà không đổi tính năng.
* `fix:` (Bug Fix) - Sửa lỗi script hoặc sửa lỗi Material.
