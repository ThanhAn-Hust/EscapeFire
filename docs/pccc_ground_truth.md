# Bảng chân lý PCCC của EscapeFire (V1.1)

Mọi bước thao tác, điểm số và câu AI Mentor trong game phải dựa trên một luật trong bảng này.
Dữ liệu máy đọc nằm ở `Assets/Resources/PcccRules.json` (được `PcccRuleBook` nạp và hiện nguồn trong màn AAR).
Test `PcccRuleBookTests` bảo đảm không có tiêu chí chấm điểm, câu Socratic hay luật AI trích dẫn nào thiếu nguồn.

| Mã | Luật | Dùng ở đâu | Nguồn |
| :--- | :--- | :--- | :--- |
| R01 | Báo cháy sớm: hô hoán, nhấn nút báo cháy, gọi **114** | điểm `alarm`, Socratic "chưa báo động", trạm 4 | [Luật 55/2024/QH15](https://luatvietnam.vn/tin-van-ban-moi/da-co-luat-phong-chay-chua-chay-va-cuu-nan-cuu-ho-cua-quoc-hoi-so-55-2024-qh15-186-100286-article.html) (hiệu lực 01/7/2025), [OSHA](https://osha.prod.pace.dol.gov/etools/evacuation-plans-procedures/emergency-standards/portable-extinguishers/use) |
| R02 | Cháy điện: **ngắt điện trước**, không dùng nước/bọt lên thiết bị có điện | điểm `power_cut`, Socratic, AI (NFPA 75 8.4) | [NFPA 75](https://www.nfpa.org/codes-and-standards/nfpa-75-standard-development/75), [SGGP](https://www.sggp.org.vn/thoat-nan-khi-chay-cao-oc-post471983.html) |
| R03 | Chọn đúng bình: CO2/bột cho cháy điện | điểm `agent`, trạm 2 | [NFPA 10](https://www.nfpa.org/codes-and-standards/nfpa-10-standard-development/10), [MT5](https://thietbipcccbinhduong.com/binh-chua-chay-co2-mt5/) |
| R04 | **PASS**: rút chốt, nhắm **gốc** lửa, bóp, **quét**; đứng cách **1,8–2,4 m** | điểm `technique`, `FireSuppressionModel`, trạm 3 | [OSHA PASS](https://osha.prod.pace.dol.gov/etools/evacuation-plans-procedures/emergency-standards/portable-extinguishers/use), [Clark County FD](https://clarkcountynv.gov/government/departments/fire/fire-extinguishers) |
| R05 | Bình CO2 MT5 5 kg: phun **≥ 10 s**, tầm **≥ 2 m** (bình EU 13–15 s) → mô phỏng **12 s** | `FireExtinguisher.fullChargeSeconds` | [MT5](https://thietbipcccbinhduong.com/binh-chua-chay-co2-mt5/), [Safelincs](https://safelincs.co.uk/ultrafire-5kg-co2-fire-extinguisher) |
| R06 | Bình hết hoặc cháy lớn → **ngừng dập, sơ tán** | Mentor khi bình hết, AI (FM-200) | [OSHA](https://osha.prod.pace.dol.gov/etools/evacuation-plans-procedures/emergency-standards/portable-extinguishers/use) |
| R07 | Cúi thấp/bò, không khí sạch ở ~30–60 cm sát sàn, che mũi miệng | điểm `crouch`, `PostureRules`, trạm 5 | [USFA Get Out Safely](https://www.fortworthtexas.gov/files/assets/public/fire/documents/getoutsafely.pdf), [SGGP](https://www.sggp.org.vn/bo-tui-ngay-ky-nang-thoat-hiem-hoa-hoan-khi-sinh-song-tai-chung-cu-cao-tang-post710988.html) |
| R08 | Kiểm tra cửa bằng **mu bàn tay** (đỉnh cửa, tay nắm, khe cửa); nóng thì dùng lối khác | `DoorHeatChecker`, Socratic, AI (NFPA 101) | [USFA](https://www.fortworthtexas.gov/files/assets/public/fire/documents/getoutsafely.pdf), [FEMA](https://webarchive.library.unt.edu/web/20080916061555/http://www.fema.gov/hazard/fire/fire_during.shtm) |
| R09 | Người tỉnh: dìu, cả hai giữ thấp; bất tỉnh: kéo sát sàn (Rautek/chăn), giữ đầu thấp | điểm `rescue`, `EscortGrip` | [Fire Engineering](https://www.fireengineering.com/firefighting/civilian-rescue-removing-the-occupant/), [Miami-Dade](https://mdsceh.miamidade.gov/mobi/moms/section%2006%20-%20victim%20removal%20techniques.pdf) |
| R10 | **Không dùng thang máy**; cầu thang bộ theo EXIT, tay tựa tường/lan can | trạm 5, V2 | [TDI Texas](https://Www.Tdi.Texas.Gov/pubs/videoresource/qahighrisebldge.pdf), [SGGP](https://www.sggp.org.vn/thoat-nan-khi-chay-cao-oc-post471983.html) |
| R11 | FM-200 đếm ngược → mọi người rời phòng trước khi xả khí | AI (NFPA 2001), `FM200System` | [NFPA 2001](https://www.nfpa.org/codes-and-standards/nfpa-2001-standard-development/2001) |
| R12 | Tam giác cháy: chất cháy + nhiệt + oxy; bỏ một yếu tố thì tắt | trạm 1 | [NFPA](https://www.nfpa.org/education-and-research/home-fire-safety) |

## Rà soát kịch bản (V1.3) — đã sửa

| Chỗ sai | Vì sao sai | Đã sửa |
| :--- | :--- | :--- |
| Pha "chập điện" tự cộng 10 điểm | Người học chưa làm gì | Bỏ điểm; Guided hỏi "Bạn sẽ làm gì trước tiên?" |
| Mentor đọc nguyên quy trình cả ở chế độ Sát hạch | Sát hạch phải tự biết làm (B.0-8) | Exam chỉ báo "Có cháy ở tủ server!" |
| Điểm "kỹ thuật" chỉ xét rút chốt + dập tắt | R04: phải nhắm gốc và quét | Chấm theo số đo thật: rút chốt 3 + nhắm gốc 4 + quét 3 + dập tắt 5 |
| Bình xả 15 s | R05: MT5 ≥ 10 s | 12 s (CO2), 40 s (nước) |
| Cửa Lab đặt "nóng", chỉ cộng điểm khi nóng | R08: kiểm tra luôn là đúng; lửa ở trong phòng nên tay nắm nguội | Đã sửa ở 11/11 |
| Cứu Hoa: Hoa tự đứng dậy đi ra | R09: phải dìu | V0.8: dìu bằng tay |
| Rút chốt/ngắt điện/mở cửa bằng "chọn" từ xa | Thao tác thật bằng tay | V0.3–V0.7 |

## Còn ước lượng (chưa có nguồn số liệu riêng)

- Ngưỡng tư thế (khom < 80 % H₀, bò < 55 % H₀) và tốc độ đi theo tư thế: lựa chọn thiết kế, đối chiếu R07 ở mức định tính.
- Góc nón phun 15°, ngưỡng quét 10°/s, hệ số "phun vào ngọn lửa" 0,2: lựa chọn thiết kế để chấm PASS.
- Thông số bình thay đổi theo nhà sản xuất: nên đối chiếu tem nhãn bình thật của phòng Lab nếu dùng để nghiệm thu.
