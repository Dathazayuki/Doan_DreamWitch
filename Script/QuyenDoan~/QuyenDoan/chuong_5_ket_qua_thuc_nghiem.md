# CHƯƠNG 5: KẾT QUẢ THỰC NGHIỆM

---

## 5.1 Tổng quan kỹ thuật
Phần này đặc tả các thông số kỹ thuật cốt lõi và cấu hình phần cứng yêu cầu để vận hành trò chơi "Dream Witch". Nhằm đảm bảo trải nghiệm chơi game đi cảnh hành động nhịp độ cao diễn ra mượt mà nhất, trò chơi hướng tới mục tiêu hiệu năng đạt mức **60 FPS** ổn định trên cấu hình khuyến nghị. Mức khung hình này giúp đồng bộ hóa tối ưu giữa chuỗi xử lý lệnh di chuyển và phản hồi hoạt ảnh của nhân vật, giảm thiểu tối đa hiện tượng trễ hình (input lag) hay giật xé hình khi người chơi thực hiện các thao tác di chuyển phức tạp.

Hệ thống yêu cầu về phần cứng máy tính tối thiểu và khuyến nghị để vận hành trò chơi được chi tiết hóa trong bảng dưới đây:

### Bảng 5.1: Bảng yêu cầu cấu hình phần cứng tối thiểu và khuyến nghị
| Thành phần phần cứng | Cấu hình tối thiểu (Minimum) | Cấu hình khuyến nghị (Recommended) |
| :--- | :--- | :--- |
| **Hệ điều hành (OS)** | Windows 10/11 (64-bit) | Windows 10/11 (64-bit) |
| **Bộ vi xử lý (CPU)** | Intel Core i3-4160 hoặc AMD Phenom II X4 965 | Intel Core i5-6400 hoặc AMD Ryzen 3 1200 |
| **Bộ bộ nhớ trong (RAM)** | 4 GB RAM | 8 GB RAM |
| **Card đồ họa (GPU)** | NVIDIA GeForce GTX 560 hoặc AMD Radeon HD 5770 (1GB VRAM) | NVIDIA GeForce GTX 960 hoặc AMD Radeon R9 380 (2GB VRAM) |
| **Phiên bản DirectX** | DirectX 11 | DirectX 11 |
| **Dung lượng ổ cứng** | 1 GB dung lượng trống | 1 GB dung lượng trống (ưu tiên SSD) |

---

## 5.2 Thiết kế kiến trúc

### 5.2.1 Lựa chọn kiến trúc phần mềm
Để phát triển hệ thống điều khiển nhân vật và quái vật linh hoạt trong thể loại Metroidvania hành động nhịp độ cao, đề tài lựa chọn áp dụng mô hình **Kiến trúc hướng thành phần (Component-Based Architecture)** đặc thù của Unity Engine kết hợp với **Mẫu thiết kế Trung gian (Mediator Pattern)** và **Kiến trúc dữ liệu hướng cấu hình (Data-Driven Design)** để tổ chức cấu trúc lớp trong trò chơi.

Trước khi đi sâu vào thiết kế thực thể người chơi, cần làm rõ hai khái niệm cốt lõi của Kiến trúc hướng thành phần trong Unity:
- **GameObject (Đối tượng trò chơi):** Là thực thể cơ sở và là container trống đóng vai trò chứa đựng các thành phần logic. Bản thân GameObject không tự sở hữu bất kỳ hành vi hay dữ liệu nào ngoại trừ các thông số vật lý cơ bản về vị trí, góc xoay và tỉ lệ (được định nghĩa trong component mặc định `Transform`). Tất cả các thực thể trong trò chơi như nhân vật chính, kẻ địch, cạm bẫy hay camera đều được biểu diễn dưới dạng GameObject.
- **Component (Thành phần chức năng):** Là các khối logic mã nguồn độc lập (kế thừa từ lớp cơ sở `MonoBehaviour` của Unity) được gắn trực tiếp vào GameObject để cung cấp các thuộc tính và hành vi chuyên biệt. Mô hình này tuân thủ nguyên lý thiết kế ưu tiên lắp ghép hơn kế thừa sâu lớp (Composition over Inheritance). Bằng việc kết hợp các thành phần khác nhau, nhà phát triển có thể tạo ra các thực thể có hành vi phong phú mà không gặp phải sự cứng nhắc và phức tạp của cây kế thừa lớp truyền thống.

Áp dụng nguyên lý trên vào thực thể chính của người chơi (`GameObject` Player), hệ thống được lắp ghép từ các component hoạt động độc lập dưới đây:
- `PlayerInput`: Chịu trách nhiệm đọc, lọc nhiễu và lưu vào bộ đệm các tín hiệu nhập liệu bàn phím/chuột từ người dùng (ví dụ: Jump buffering để đệm lệnh nhảy trước khi chạm đất, Dash buffering).
- `PlayerMovement`: Xử lý động học, tính toán lực nhảy, di chuyển, lướt 8 hướng, bám vách tường và tương tác vật lý với thế giới thông qua Rigidbody2D.
- `PlayerStats`: Quản lý các chỉ số sinh tồn runtime thay đổi liên tục (Máu, Thể lực, Năng lượng) và kích hoạt các sự kiện C# event khi các chỉ số này thay đổi.
- `PlayerCollision`: Chịu trách nhiệm kiểm tra va chạm vật lý với môi trường địa hình, vùng sát thương của bẫy gai và nhận diện các trigger an toàn.
- `PlayerCombat`: Quản lý trạng thái chiến đấu, chuỗi combo 3-hit cận chiến và kích hoạt bật/tắt các hitbox tấn công động.
- `PlayerFormManager`: Quản lý cơ chế biến đổi linh hồn, thực hiện nạp và hủy động các cấu trúc Prefab vật lý của hình thái biến đổi.

Để các component trên giao tiếp với nhau hiệu quả mà không bị liên kết chặt (tight coupling), trò chơi áp dụng **Mẫu thiết kế Trung gian (Mediator Pattern)**:
- Lớp `PlayerController` đóng vai trò là Mediator trung tâm. Trong pha khởi tạo (`Awake`), lớp này lưu trữ tham chiếu đến tất cả các component con trên.
- Khi có sự kiện phát sinh, các component con không gọi trực tiếp lẫn nhau mà gửi thông điệp thông qua `PlayerController`. Ví dụ, khi `PlayerInput` nhận tín hiệu nhảy, nó thông báo cho `PlayerController`, từ đó điều phối gọi phương thức nhảy trong `PlayerMovement` chỉ khi `PlayerCollision` xác nhận nhân vật đang đứng trên mặt đất.

Đồng thời, trò chơi áp dụng **Kiến trúc dữ liệu hướng cấu hình (Data-Driven Design)** thông qua việc sử dụng **ScriptableObject**:
- ScriptableObject là một thùng chứa dữ liệu tĩnh đặc thù của Unity, tồn tại độc lập dưới dạng tài sản tệp tin trong bộ nhớ dự án mà không cần gắn vào bất kỳ GameObject nào.
- Việc áp dụng ScriptableObject (như lớp `PlayerFormDataSO` cấu hình thuộc tính linh hồn, `ItemDatabaseSO` định nghĩa danh sách vật phẩm, `SpellDatabaseSO` chứa các kỹ năng phép thuật) giúp phân tách hoàn toàn dữ liệu cân bằng trò chơi ra khỏi logic xử lý. Nhà thiết kế game có thể trực tiếp tinh chỉnh các thông số như HP tối đa, tốc độ chạy, hoặc gán các prefab vật lý khác nhau trực tiếp trên giao diện Unity Inspector mà không cần sửa đổi hay biên dịch lại mã nguồn hệ thống.

---

### 5.2.2 Thiết kế tổng quan

Để tổ chức mã nguồn rõ ràng và dễ bảo trì, kiến trúc phần mềm của trò chơi được phân rã thành các phân hệ. Sơ đồ phân rã các phân hệ chính trong mã nguồn trò chơi được trình bày tại Hình 5.1.

*   **Hình 5.1: Sơ đồ phân rã các phân hệ chính trong mã nguồn trò chơi**
    ![system_decomposition](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/system_decomposition.png)

*Giải thích Sơ đồ phân rã phân hệ (Hình 5.1):*
Mã nguồn trò chơi được tổ chức theo mô hình phân tầng nghiêm ngặt từ trên xuống dưới nhằm triệt tiêu các phụ thuộc vòng:
- **Tầng Giao diện (View Layer - Gói UI):** Chứa giao diện người dùng hiển thị thông tin như HUD máu, cửa hàng, bản đồ. Tầng này chỉ phụ thuộc vào tầng điều phối bên dưới để lấy dữ liệu hiển thị thông qua các sự kiện, hoàn toàn không can thiệp vào vật lý hay logic lõi.
- **Tầng Logic & Điều phối (Controller/Logic Layer):** Chứa toàn bộ logic xử lý nghiệp vụ của game, phân chia thành các gói chức năng hoạt động độc lập và chỉ giao tiếp gián tiếp qua các giao diện định nghĩa sẵn để tránh kết hợp cứng. Gói này bao gồm các gói con như Player, Enemy, System, Trap, VFX, và gói tương tác Interact Object.
- **Tầng Dữ liệu & Hạ tầng (Model/Utility / SO Layer):** Định nghĩa các hằng số, mẫu Singleton dùng chung, các giao diện trừu tượng, và lưu trữ dữ liệu cấu hình tĩnh cũng như trạng thái chạy của trò chơi thông qua các ScriptableObjects. Gói này hoàn toàn độc lập, nằm ở đáy hệ thống.

Hệ thống mã nguồn trò chơi được chia thành 10 gói chức năng chính cụ thể như sau:
-   **Gói Player:** Quản lý toàn bộ vòng đời, máy trạng thái di chuyển, hoạt ảnh và các hành vi vật lý/chiến đấu của nhân vật chính. Gói chứa các thành phần con xử lý di chuyển nền tảng (Platforming), lướt né tránh (Dash), nhảy đôi, bám vách leo tường và cơ chế chiến đấu cận chiến 3-hit combo. Đồng thời, gói cũng điều khiển hệ thống chuyển đổi hình thái linh hồn (Form Switch), giúp nhân vật thay đổi cấu hình Animator và Prefab vật lý theo từng dạng quái vật hấp thụ được.
-   **Gói Enemy:** Thiết lập và điều phối hành vi của toàn bộ kẻ địch thông thường cùng các thực thể Boss lớn trong trò chơi. Phân hệ này sử dụng cấu trúc AI Brain kết hợp máy trạng thái (StateMachine) để quái vật tuần tra ngẫu nhiên, chuyển sang trạng thái cảnh giác khi nghe tiếng động và truy đuổi người chơi khi bước vào tầm quét. Đối với Boss, gói quản lý trọng số mong muốn (Desire Weights) và các chuỗi chiêu thức liên hoàn (Combo Patterns) phức tạp.
-   **Gói System:** Quản lý các dịch vụ và hệ thống nền tảng phục vụ cho vận hành game cục bộ. Gói bao gồm phân hệ lưu trữ và tải game (Save/Load) dưới định dạng JSON bền vững trên thiết bị, hệ thống nâng cấp làng và các thuộc tính vĩnh viễn của nhân vật (Facility), trạng thái mua sắm tại NPC Shopkeeper (Shop) và hệ thống tối ưu hóa hiệu năng culling tắt/bật phòng chơi theo tầm nhìn camera.
-   **Gói UI:** Quản lý các Canvas giao diện hiển thị tĩnh và động trong trò chơi. HUD Canvas hiển thị lượng máu, thể lực, năng lượng thời gian thực của người chơi. Panels Canvas hiển thị các bảng menu chính, menu lưu trữ, túi đồ, cây kỹ năng nâng cao và bản đồ định vị thế giới. World Canvas được sử dụng để hiển thị các thành phần UI động như chữ sát thương bay (Damage Text) tại tọa độ không gian thế giới.
-   **Gói Interfaces:** Định nghĩa các hợp đồng giao tiếp trừu tượng phục vụ cho việc liên kết lỏng (loose coupling) giữa các phân hệ. Tiêu biểu là giao diện nhận sát thương (`IDamageable`) được thực thi bởi cả người chơi và quái vật để nhận lực đẩy lùi và giảm máu, cùng giao diện tương tác (`IInteractable`) cho phép người chơi tương tác với cửa, hòm đồ, xác quái vật và đền thờ.
-   **Gói Core:** Đóng vai trò là nền tảng khởi tạo và điều phối dòng chạy chính của trò chơi. Gói chứa các cấu hình hằng số toàn cục, các lớp quản lý theo mẫu Singleton (như GameManager, UIManager, GamePauseManager), và các tiện ích dùng chung xuyên suốt các màn chơi để đồng nhất dữ liệu.
-   **Gói Trap:** Quản lý các đối tượng cạm bẫy vật lý được bố trí trong môi trường bản đồ như bẫy gai nhọn, lưỡi dao xoay. Các lớp trong gói này chịu trách nhiệm kiểm tra va chạm vật lý với người chơi và kích hoạt trừ máu trực tiếp thông qua giao diện nhận sát thương, góp phần tạo nên thử thách đi cảnh.
-   **Gói VFX:** Quản lý toàn bộ hiệu ứng hình ảnh, hoạt ảnh phản hồi và các hệ thống hạt (Particle System) trong game. Phân hệ này kích hoạt các hiệu ứng đặc biệt khi người chơi lướt né đòn, thực hiện combo chém cận chiến, thi triển đạn phép thuật hoặc khi quái vật tan biến khi bị tiêu diệt, giúp tăng trải nghiệm thị giác.
-   **Gói Scriptable Object:** Lưu trữ và quản lý toàn bộ cấu trúc dữ liệu hướng cấu hình (Data-Driven Design) của dự án. Phân hệ này chứa các tài sản dữ liệu tĩnh như cơ sở dữ liệu vật phẩm, phép thuật, cây kỹ năng, cấu hình hình thái linh hồn (Form Switch), và các trạng thái chạy runtime dùng chung như ví tiền vàng, trạng thái hòm đồ nâng cấp.
-   **Gói Interact Object:** Quản lý các đối tượng tương tác vật lý trong thế giới game thông qua va chạm trigger 2D. Phân hệ bao gồm các điểm nhặt vật phẩm rơi ra (Item Pickup), rương sách phép thuật (Spell Book Chest), rương lưu trữ đồ (Storage), cần gạt kích hoạt thang máy, và các vùng hiển thị gợi ý phím tương tác lên màn hình HUD của người chơi.

Tiếp theo, sơ đồ kiến trúc luồng dữ liệu biểu thị các liên kết tương tác giữa các phân hệ được trình bày tại Hình 5.2.

*   **Hình 5.2: Kiến trúc tổng thể các phân hệ và lớp logic trong hệ thống**
    ![game_architecture](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/game_architecture.png)

*Giải thích Sơ đồ kiến trúc tổng thể các lớp logic (Hình 5.2):*
Luồng dữ liệu và tương tác vật lý diễn ra nhịp nhàng giữa các tầng:
- Khi người chơi thao tác, thành phần `PlayerInput` thu nhận tín hiệu và gửi tới bộ điều phối trung tâm `PlayerController`.
- `PlayerController` đóng vai trò Mediator ra lệnh cho `PlayerMovement` áp lực vật lý, hoặc yêu cầu `PlayerFormManager` tải dữ liệu cấu hình từ các `ScriptableObjects` để biến đổi hình thái.
- Khi máu nhân vật thay đổi, `PlayerStats` phát đi sự kiện dạng event-driven để cập nhật lập tức lên màn hình `PlayerHudUI`.
- `GameSaveManager` chịu trách nhiệm thu thập trạng thái từ các component này để thực hiện tuần tự hóa, trong khi `CullingManager` vô hiệu hóa các đối tượng nằm ngoài tầm nhìn nhằm tối ưu hóa hiệu năng vẽ.

---

### 5.2.3 Thiết kế chi tiết gói

Dưới đây là biểu đồ thiết kế gói chi tiết cho các gói cốt lõi của hệ thống bao gồm: gói Player (Hình 5.3), gói Enemy (Hình 5.4), gói UI (Hình 5.5), gói System (Hình 5.6), gói Scriptable Object (Hình 5.7), gói Interact Object (Hình 5.8), và biểu đồ phân cấp kẻ địch (Hình 5.9).

*   **Hình 5.3: Biểu đồ thiết kế chi tiết bên trong gói Player (Player Package Design)**
    ![player_package_design](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/player_package_design.png)

*Giải thích biểu đồ gói Player (Hình 5.3):*
Biểu đồ mô tả cấu trúc lắp ghép và các mối quan hệ lớp chuẩn UML bên trong phân hệ người chơi:
- Lớp điều phối trung tâm `PlayerController` duy trì mối quan hệ Hợp thành (Composition) với máy trạng thái `PlayerStateMachine`.
- `PlayerController` liên kết thông qua quan hệ Kết hợp (Association) đến các component độc lập (`PlayerInput`, `PlayerMovement`, `PlayerStats`, `PlayerFormManager`) để phân bổ nhiệm vụ xử lý di chuyển, chỉ số và biến hình.
- `PlayerFormManager` sở hữu quan hệ Kết tập (Aggregation) với các đối tượng cấu hình `PlayerFormDataSO` kế thừa từ ScriptableObject.
- `PlayerController` thực thi (Implementation) giao diện trừu tượng `IDamageable` từ gói `Interfaces` để cho phép nhận sát thương đồng nhất.

*   **Hình 5.4: Biểu đồ thiết kế chi tiết bên trong gói Enemy (Enemy Package Design)**
    ![enemy_package_design](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/enemy_package_design.png)

*Giải thích biểu đồ gói Enemy (Hình 5.4):*
Biểu đồ đặc tả cấu trúc máy trạng thái AI dùng chung cho toàn bộ quái vật:
- Lớp cơ sở trừu tượng `MvEnemyBase` kế thừa từ `MonoBehaviour` của Unity, thực thi giao diện nhận sát thương `IDamageable` và sở hữu mối quan hệ Hợp thành (Composition) với `EnemyStateMachine`.
- `EnemyStateMachine` quản lý danh sách các trạng thái `EnemyState` độc lập. Mỗi `EnemyState` duy trì một tham chiếu đến `EnemyContext`.
- Lớp `MvEnemyBase` kết hợp (Association) với thành phần xử lý hoạt ảnh `EnemyAnimationController` và lớp tấn công vật lý `MvAttack`.
- Các lớp kẻ địch cụ thể như `MvEm0010` (BoneDog) sử dụng quan hệ Kế thừa (Inheritance) từ `MvEnemyBase` để tái sử dụng toàn bộ khung AI.

*   **Hình 5.5: Biểu đồ thiết kế chi tiết bên trong gói UI (UI Package Design)**
    ![ui_package_design](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/ui_package_design.png)

*Giải thích biểu đồ gói UI (Hình 5.5):*
Biểu đồ mô tả cấu trúc phân lớp Canvas và quản lý giao diện người dùng:
- Lớp điều phối trung tâm `UIManager` sở hữu mối quan hệ Hợp thành (Composition) với `UIStateManager` để quản lý máy trạng thái hiển thị của UI.
- `UIManager` duy trì quan hệ kết hợp đến các Canvas giao diện hiển thị chuyên biệt: `PlayerHudUI` (HUD máu, thể lực), `SaveSlotMenuUI` (Menu chọn tệp lưu trữ), và `TitleCanvasManager` (Màn hình chính bắt đầu game).
- `SaveSlotMenuUI` liên kết với mảng các nút chọn `SaveSlotButtonView`.
- `TitleCanvasManager` kết hợp với thành phần thiết lập âm lượng `TitleSettingMenuController`.
- Lớp `DamageTextView` đại diện cho UI hiển thị chữ sát thương bay trong không gian 2D.

*   **Hình 5.6: Biểu đồ thiết kế chi tiết bên trong gói System (System Package Design)**
    ![system_package_design](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/system_package_design.png)

*Giải thích biểu đồ gói System (Hình 5.6):*
Biểu đồ mô tả hệ thống quản lý lưu trữ và tiến trình của trò chơi:
- Lớp quản lý lưu trữ `GameSaveManager` thiết kế theo mẫu Singleton duy trì mối quan hệ phụ thuộc với cấu trúc dữ liệu `GameSaveData` để thực hiện serialize/deserialize JSON.
- `GameSaveManager` kết hợp (Association) với các ScriptableObject trạng thái động như `CurrencyWalletSO` (ví tiền vàng) và `InventoryStateSO` (túi đồ), đồng thời phụ thuộc vào lớp dịch vụ tĩnh `PortalCheckpointService` để ghi nhận các điểm dịch chuyển đã mở khóa.

*   **Hình 5.7: Biểu đồ thiết kế chi tiết bên trong gói Scriptable Object (Scriptable Object Package Design)**
    ![scriptable_object_package_design](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/scriptable_object_package_design.png)

*Giải thích biểu đồ gói Scriptable Object (Hình 5.7):*
Biểu đồ mô tả cấu trúc phân lớp dữ liệu hướng cấu hình ScriptableObject trong game:
- Lớp cơ sở `ScriptableObject` cung cấp các thuộc tính quản lý bộ nhớ của Unity Engine.
- Các lớp dữ liệu cấu hình tĩnh (như `ItemDefinitionSO` định nghĩa thuộc tính vật phẩm, `KeyItemSO` và `ToolItemSO` kế thừa để phân biệt loại vật phẩm, `SpellDatabaseSO` và `SpellData` chứa danh sách phép thuật, `SkillTreeDatabaseSO` chứa nhánh kỹ năng, `FacilityUpgradeDatabaseSO` chứa thông tin nâng cấp làng) tồn tại dưới dạng tài sản độc lập trong bộ nhớ.
- Các lớp lưu trữ trạng thái chạy runtime (như `InventoryStateSO` quản lý số lượng vật phẩm trong túi đồ, `CurrencyWalletSO` quản lý số vàng, `FacilityProgressSO` quản lý cấp độ nâng cấp làng hiện tại) giúp các phân hệ truy xuất nhanh mà không phụ thuộc lẫn nhau.

*   **Hình 5.8: Biểu đồ thiết kế chi tiết bên trong gói Interact Object (Interact Object Package Design)**
    ![interact_object_package_design](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/interact_object_package_design.png)

*Giải thích biểu đồ gói Interact Object (Hình 5.8):*
Biểu đồ mô tả cấu trúc thiết kế của phân hệ tương tác trong trò chơi:
- Giao diện `IInteractable` trong gói Interfaces định nghĩa các hàm chung cho các thực thể tương tác bao gồm: `Interact` (thực thi hành động), `GetInteractPrompt` (lấy văn bản chỉ dẫn), và `CanInteract` (kiểm tra điều kiện tương tác).
- Các lớp tương tác cụ thể kế thừa từ `MonoBehaviour` và quản lý va chạm thông qua các trigger vật lý 2D. `ItemPickup` (quản lý nhặt vật phẩm và lưu trạng thái thu thập qua `WorldPickupSaveService`), `SpellBookChest` (mở hòm sách phép thuật), `Storage` (mở rương quản lý túi đồ), `ElevatorLever` (gạt cần kích hoạt thang máy), và `InteractPromptTrigger` (hiển thị gợi ý phím bấm).
- Các lớp tương tác này phụ thuộc trực tiếp vào `UIManager` để gửi thông điệp hiển thị phím gợi ý tương tác (`ShowInteractPrompt`) lên Canvas HUD của người chơi.

*   **Hình 5.9: Biểu đồ phân cấp kế thừa các thực thể Kẻ địch (Enemy Class Hierarchy)**
    ![enemy_class_hierarchy](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/enemy_class_hierarchy.png)

*Giải thích biểu đồ kế thừa kẻ địch (Hình 5.9):*
Biểu đồ thể hiện mối quan hệ kế thừa trực tiếp từ lớp cơ sở trừu tượng `MvEnemyBase`. Các thực thể quái vật thông thường (`MvEm0010` - BoneDog, `MvEm0060` - ShieldKnight, `MvEm0070` - FlyingMage, `MvEm0100` - Brawler) cùng các Boss lớn có AI phức tạp hơn (`MvEm9020` - Golem Boss, `MvEm9030` - Marionette Boss) đều kế thừa trực tiếp từ lớp cơ sở này để đảm bảo tính đồng nhất của hệ thống vật lý và nhận sát thương.

---

### 5.2.4 Cấu trúc chi tiết các phân hệ cốt lõi

#### a. Phân hệ Người chơi và Cơ chế Biến hình (Form Switch)
Cơ chế biến hình cho phép người chơi thay đổi linh hoạt hình dạng của nhân vật chính sang các dạng quái vật khác khi thu thập linh hồn. Hệ thống này sử dụng cấu trúc dữ liệu tĩnh hướng cấu hình (Data-Driven Design) thông qua các lớp:
- `PlayerFormId` (Enum): Định danh các hình thái linh hồn: `Human` (Dạng người mặc định), `Em0010` (BoneDog), `Em0020` (GhostCat), `Em0060` (ShieldKnight), `Em0070` (FlyingMage), và `Em0100` (Brawler).
- `PlayerFormDataSO` (ScriptableObject): Lưu trữ các dữ liệu thiết kế cố định như tên hiển thị, icon HUD, lượng HP tối đa, Animator Controller và Prefab vật lý tương ứng của từng hình thái.

Khi người chơi kích hoạt chuyển đổi linh hồn gần một xác chết kẻ địch, lớp điều phối trung tâm `PlayerController` kết hợp với `PlayerFormManager` thực hiện chuỗi hành vi đồng bộ hóa thời gian thực:
1.  **Đồng bộ hóa Chỉ số Sinh lực:** Lưu chỉ số HP hiện tại của form cũ và thiết lập chỉ số HP tương ứng của form mới vào lớp `PlayerStats`.
2.  **Tải cấu trúc vật lý động:** Lớp `PlayerFormManager` hủy phiên bản Prefab cũ và khởi tạo (`Instantiate`) Prefab của form mới làm con của Player Root. Prefab này được đính kèm component `PlayerFormBodyRef` định nghĩa rõ ràng các collider va chạm môi trường và các trigger hitbox tấn công cận chiến đặc thù.
3.  **Đồng bộ hóa Collider trong Physics Engine:** Lớp `PlayerMovement` tiếp nhận thông tin từ `PlayerFormBodyRef` mới để gán lại Collider va chạm thực tế và các điểm kiểm tra mặt đất (`GroundCheck`), vách tường (`WallCheck`) vào Physics2D của Unity, tránh lỗi kẹt địa hình do chênh lệch kích thước giữa các hình thái.
4.  **Rebind hoạt ảnh:** Thay đổi runtime controller của thành phần `Animator` trên Player Root bằng Animator Controller của form mới để kích hoạt tức thì các clip hoạt ảnh tương ứng. Hệ thống quản lý hoạt ảnh của nhân vật chính và các quái vật thông thường thực hiện gọi trực tiếp các hoạt ảnh này thông qua chuỗi ký tự (string-based loading) thay vị sử dụng các tham số và quan hệ chuyển cảnh phức tạp trong Animator Controller.
5.  **Cập nhật chỉ số chiến đấu:** Thay đổi cấu hình sát thương và chuỗi combo cận chiến trong `PlayerCombat` theo thông số của form mới.

Để tránh việc lớp `PlayerController` bị phình to do phải quản lý hàng chục trạng thái của nhiều form khác nhau, hệ thống sử dụng mẫu thiết kế Factory thông qua lớp `FormStateFactory`. Khi người chơi ở dạng người mặc định, `PlayerStateMachine` sử dụng các trạng thái tĩnh được khởi tạo từ đầu (như `IdleState`, `MoveState`, `JumpState`, `DashState`, `WallClimbState`). Khi chuyển sang một dạng Enemy biến hình, `FormStateFactory` sẽ tạo ra theo nhu cầu (on-demand) một bộ trạng thái chuyên biệt cho form đó (ví dụ: `Em0010IdleState`, `Em0010AttackState`) kế thừa từ lớp cơ sở `PlayerState`.

#### b. Phân hệ Kẻ địch và Trí tuệ nhân tạo (AI Brain)
Hệ thống quái vật trong game được thiết kế hướng đối tượng chặt chẽ nhằm tối ưu hóa việc quản lý mã nguồn di chuyển, phát hiện mục tiêu và va chạm vật lý:
- **Lớp cơ sở trừu tượng `MvEnemyBase`:** Kế thừa trực tiếp từ `MonoBehaviour` và thực thi giao diện `IDamageable`. Lớp này chịu trách nhiệm quản lý máy trạng thái `EnemyStateMachine`, quản lý các chỉ số cơ bản (Máu, Tốc độ, Tầm đánh), xử lý các hiệu ứng vật lý (lực đẩy lùi - Knockback, đệm va chạm), và tích hợp hệ thống tự động tìm kiếm mục tiêu (`UpdateSearchTarget`) trong phạm vi quạt góc quét (`searchFanAngle`) và tầm quét (`searchRadius`). Đồng thời, lớp này khai báo cấu trúc enum `AsCommon` đại diện cho các trạng thái dùng chung của quái vật (Idle, Run, Turn, Hit, Death).
- **Các lớp quái vật và Boss cụ thể (`MvEmXXXX`):** Kế thừa từ `MvEnemyBase` (ví dụ: `MvEm0010` - BoneDog, `MvEm9020` - Golem Boss, `MvEm9030` - Marionette Boss). Các lớp này override các phương thức đặc thù như thiết lập kiểu quái vật (`EmType`), ánh xạ tên trạng thái, đăng ký bảng trạng thái và tùy biến thuật toán quét tìm người chơi.

Các trận chiến với Boss lớn được điều phối bởi lớp bộ não AI chuyên biệt `MvEmBrain` chạy song song với máy trạng thái:
- **Trọng số mong muốn (Desire Weights):** AI liên tục đánh giá khoảng cách đến người chơi và phần trăm máu còn lại của Boss để cập nhật bảng trọng số mong muốn của các chiêu thức (ví dụ: khi người chơi ở xa, trọng số chiêu phóng đạn tăng; khi Boss dưới 50% máu, trọng số các đòn tấn công nhanh, diện rộng tăng).
- **Chuỗi chiêu thức (Combo Patterns):** Thay vì ra đòn ngẫu nhiên, Boss chọn một chuỗi hành động kết hợp được lập trình sẵn (`ComboBuff` lưu mảng các trạng thái hành vi). Khi combo bắt đầu, Boss sẽ thực thi tuần tự tất cả các đòn đánh trong combo đó trước khi quay lại trạng thái đánh giá mong muốn tiếp theo. Sơ đồ luồng quyết định hành động của Boss AI được mô tả chi tiết tại Hình 5.10.

*   **Hình 5.10: Sơ đồ luồng ra quyết định hành động của Boss AI dựa trên Desire và Combo**
    ![boss_ai_flow](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/BossAiFlow.png)

*Giải thích Sơ đồ luồng quyết định của Boss AI (Hình 5.10):*
Sơ đồ minh họa quá trình tính toán và ra quyết định hành vi của bộ não Boss AI:
1. Hệ thống AI liên tục đo đạc khoảng cách vật lý từ vị trí Boss đến người chơi và phần trăm lượng máu còn lại của Boss.
2. Từ các chỉ số runtime này, AI thực hiện cập nhật bảng trọng số mong muốn (Desire Weights) của các chiêu thức có sẵn trong cơ sở dữ liệu.
3. Nếu Boss đang không thực thi một chuỗi chiêu thức cụ thể, AI sẽ lựa chọn chiêu thức có trọng số mong muốn cao nhất để thi triển.
4. Nếu chiêu thức được chọn là một đòn đánh đơn lẻ, Boss sẽ thực thi đòn đánh và quay lại pha đánh giá. Nếu chiêu thức kích hoạt một chuỗi combo (Combo Pattern), Boss sẽ chạy tuần tự toàn bộ mảng trạng thái hành vi được lưu trong `ComboBuff` (ví dụ: lướt tới áp sát, vung kiếm quét rộng, rồi đập đất tạo chấn động) trước khi cho phép AI thực hiện pha đánh giá tiếp theo.

#### c. Các hệ thống lõi phục vụ vận hành

- **Hệ thống Lưu trữ (Save/Load):** Sử dụng lớp `GameSaveManager` để serialize trạng thái trò chơi thành chuỗi dữ liệu JSON và lưu trữ lâu dài. `GameSaveManager` được thiết kế theo mẫu Singleton và nằm trên Object có thuộc tính `DontDestroyOnLoad`, đảm bảo tiến trình lưu/tải không bị gián đoạn khi chuyển cảnh. Để làm rõ các tương tác truyền tin trong quá trình lưu trữ, sơ đồ trình tự quá trình lưu trữ được đặc tả tại Hình 5.11.

*   **Hình 5.11: Sơ đồ trình tự quá trình lưu trữ tiến trình game (Save Game Sequence)**
    ![save_system_sequence](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/save_system_sequence.png)

*Giải thích Sơ đồ trình tự quá trình lưu trữ (Hình 5.11):*
Biểu đồ trình tự thể hiện sự tương tác truyền thông điệp giữa các đối tượng khi người chơi thực hiện lưu game:
1. Người chơi di chuyển lại gần Đền thờ hồi sinh (`RespawnShrine`) và nhấn phím tương tác `E`.
2. Lớp `RespawnShrine` nhận tín hiệu tương tác và gọi phương thức `SaveActiveSlot()` trỏ tới lớp quản lý `GameSaveManager`.
3. `GameSaveManager` kích hoạt hàm nội bộ `CaptureSaveData()` để tiến hành thu thập dữ liệu hiện thời từ các phân hệ khác nhau.
4. `GameSaveManager` gửi yêu cầu lấy số dư tiền vàng từ ví `CurrencyWalletSO`, lấy thông tin vật phẩm từ túi đồ `InventoryStateSO` và cấp độ nâng cấp làng từ `FacilityManager`.
5. Sau khi thu thập đầy đủ, `GameSaveManager` tiến hành serialize đối tượng dữ liệu trung gian `GameSaveData` thành một chuỗi văn bản định dạng JSON.
6. `GameSaveManager` gọi thư viện ghi tệp tin của hệ thống để ghi đè hoặc tạo mới file JSON lưu trữ cục bộ xuống ổ cứng của máy tính người chơi.
7. Khi ổ cứng phản hồi ghi tệp tin thành công, `GameSaveManager` báo cáo kết quả hoàn thành lưu trữ cho `RespawnShrine`, từ đó kích hoạt hiển thị thông báo lưu game thành công trực tiếp lên thanh giao diện HUD cho người chơi.

- **Hệ thống Culling tối ưu hóa hiệu năng (Culling System):** Bản đồ được chia thành các khu vực phòng độc lập (`RoomController`). Khi người chơi di chuyển ra khỏi phạm vi hoạt động của một phòng chơi, `CullingManager` sẽ tự động vô hiệu hóa (`SetActive(false)`) toàn bộ Sprite Renderers, Collision Colliders và tắt các script tính toán AI của quái vật thuộc phòng đó. Các đối tượng sẽ được kích hoạt lại ngay lập tức khi người chơi bước vào vùng biên giới tiếp giáp phòng, giúp giảm thiểu đáng kể số lượng lệnh vẽ (Draw Calls) và các phép tính toán vật lý không cần thiết (Hình 5.12).

*   **Hình 5.12: Hình vẽ trực quan các phòng chơi bị tắt hiển thị ngoài tầm nhìn để tối ưu hóa**
    ![culling_vis](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/CullingVisualization.png)

*Giải thích cơ chế tối ưu hóa culling phòng chơi (Hình 5.12):*
Hình vẽ trực quan minh họa giải thuật tối ưu hóa hiệu năng vẽ đồ họa và tính toán vật lý bằng cách culling (loại bỏ) các vùng không hoạt động:
1. Bản đồ thế giới game được chia nhỏ thành các khu vực phòng độc lập, mỗi phòng được quản lý bởi một `RoomController` định nghĩa biên giới kiểm tra va chạm.
2. Khi người chơi di chuyển bên trong một phòng chơi cụ thể (phòng hoạt động), camera sẽ tập trung vào vùng này. Hệ thống chỉ cho phép các Sprites, Colliders vật lý và các script điều khiển AI của quái vật thuộc phòng đó ở trạng thái kích hoạt (`SetActive(true)`).
3. Toàn bộ các phòng chơi lân cận nằm ngoài tầm nhìn camera sẽ tự động bị `CullingManager` vô hiệu hóa (`SetActive(false)`). Việc này giúp giảm số lượng lệnh gọi vẽ (Draw Calls) gửi lên GPU và loại bỏ các phép tính va chạm vật lý không cần thiết của Unity Engine, giúp duy trì hiệu năng 60 FPS ổn định trên cấu hình máy tối thiểu.

- **Hệ thống Nâng cấp và Cửa hàng:** `FacilityManager` chịu trách nhiệm đọc ghi tiến trình nâng cấp chỉ số vĩnh viễn (HP, Mana, Sát thương cận chiến) dựa trên số lượng các vật phẩm đặc biệt. `ShopStateSO` sử dụng ScriptableObject để lưu trữ danh sách các kỹ năng phép thuật và công cụ bổ trợ có bán tại NPC Shopkeeper, tự động đồng bộ hóa trạng thái đã mua/chưa mua giữa các màn chơi.

#### d. Giao diện người dùng và Canvas (UI Canvas Architecture)
Hệ thống giao diện người dùng (UI) được thiết kế dựa trên cấu trúc phân lớp Canvas của Unity Engine:
- **HUD Canvas (Screen Space - Overlay):** Quản lý các thông tin động thời gian thực hiển thị liên tục (HP, Mana, Stamina, ô phép thuật, tiền vàng). Canvas này sử dụng thành phần `Canvas Scaler` chế độ `Scale With Screen Size` với độ phân giải tham chiếu là `1920x1080`.
- **Menu & Panels Canvas (Screen Space - Overlay):** Quản lý các giao diện tĩnh (Inventory, Status, Shop, Facility, Full Map). Giao diện này được điều khiển bởi `UIManager` và `UIStateManager` để kiểm soát vòng đời và độ ưu tiên hiển thị.
- **World Space Canvas:** Sử dụng riêng biệt cho các thành phần UI động cần kết nối trực tiếp với tọa độ thế giới 2D, tiêu biểu là lớp hiển thị chữ sát thương bay `DamageTextView` xuất hiện ngay trên đầu các thực thể khi nhận sát thương chiến đấu.

Để thực hiện cơ chế tạm dừng trò chơi khi mở các bảng chức năng tĩnh, hệ thống sử dụng lớp `UIPauseRequester` gửi yêu cầu tới bộ quản lý trung tâm `GamePauseManager` để thiết lập `Time.timeScale = 0f`. Việc này dừng hoàn toàn mọi chuyển động vật lý của Rigidbody2D, các script tính toán AI quái vật và hệ thống đạn phép, đảm bảo an toàn cho nhân vật chính trong khi người chơi tương tác với giao diện.

---

## 5.3 Thiết kế chi tiết

### 5.3.1 Thiết kế lớp

Để minh họa cụ thể cấu trúc lập trình và thiết kế chi tiết thuộc tính, phương thức của 3 lớp core xương sống đóng vai trò cốt lõi trong hệ thống (`PlayerController`, `MvEnemyBase`, và `GameSaveManager`), phần này trình bày biểu đồ lớp chi tiết và đặc tả chi tiết của từng lớp (cấm sử dụng bảng biểu theo quy định định dạng).

Dưới đây là biểu đồ lớp chi tiết của ba lớp được kết xuất trực tiếp từ mã nguồn thực tế:

*   **Hình 5.13: Biểu đồ lớp chi tiết của lớp PlayerController**
    ![player_controller_class](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/player_controller_class.png)

*Giải thích biểu đồ lớp PlayerController (Hình 5.13):*
Lớp `PlayerController` quản lý và phối hợp hoạt động của thực thể nhân vật chính. Các thuộc tính và phương thức được đặc tả như sau:
- **Các thuộc tính thành viên:**
  - `stateMachine`: Đối tượng kiểu `PlayerStateMachine` điều khiển vòng đời và việc chuyển đổi trạng thái di chuyển hiện thời của nhân vật.
  - `playerInput`: Tham chiếu đến component xử lý nhập liệu từ bàn phím và chuột của người dùng.
  - `playerMovement`: Thành phần tính toán động lực học vật lý, chịu trách nhiệm áp dụng lực lên Rigidbody2D.
  - `playerStats`: Đối tượng lưu trữ các chỉ số sinh mệnh runtime của nhân vật (Máu, Thể lực, Năng lượng).
  - `playerFormManager`: Trình quản lý cấu trúc prefab vật lý của hình thái linh hồn khi chuyển dạng.
  - `currentForm`: Enum xác định nhân vật đang ở dạng người hay dạng biến hình.
- **Các phương thức xử lý:**
  - `TakeDamage(float damage)`: Phương thức thực thi từ giao diện `IDamageable` nhận lượng sát thương truyền vào, trừ trực tiếp vào máu hiện tại trong `playerStats` và kích hoạt hiệu ứng rung màn hình, giật hình.
  - `Respawn()`: Hồi sinh nhân vật tại vị trí Checkpoint đã ghi nhận gần nhất, đưa các chỉ số máu và thể lực về trạng thái đầy.
  - `ToggleTransformForm()`: Phương thức kích hoạt việc biến đổi hình dạng linh hồn tại xác quái vật gần nhất hoặc hủy bỏ chuyển dạng để quay về hình thái người.
  - `AbandonToShrine()`: Kích hoạt sự kiện đầu hàng, tự động đưa người chơi quay về đền thờ hồi sinh gần nhất.
  - `SyncPlayerMovementCollider()`: Phương thức thực hiện tính toán lại kích thước và vị trí của collider va chạm động trên `PlayerMovement` để khớp chính xác với hình thể của dạng quái vật vừa biến hình.

*   **Hình 5.14: Biểu đồ lớp chi tiết của lớp MvEnemyBase**
    ![enemy_base_class](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/enemy_base_class.png)

*Giải thích biểu đồ lớp MvEnemyBase (Hình 5.14):*
Lớp `MvEnemyBase` là lớp cơ sở trừu tượng định nghĩa khung hành vi và cấu trúc AI cho toàn bộ quái vật và Boss. Các thuộc tính và phương thức được đặc tả như sau:
- **Các thuộc tính thành viên:**
  - `maxHealth`: Lượng máu tối đa của quái vật được cấu hình tĩnh.
  - `moveSpeed`: Tốc độ tuần tra cơ sở của thực thể.
  - `enemyStateMachine`: Đối tượng máy trạng thái quản lý các trạng thái tuần tra, truy đuổi của quái vật.
  - `target`: Đối tượng `Transform` tham chiếu trực tiếp đến nhân vật chính đang bị truy đuổi.
  - `searchRadius`: Bán kính quét xung quanh thực thể để phát hiện người chơi.
  - `searchFanAngle`: Góc quét hình quạt phía trước mặt quái vật để nhận diện mục tiêu.
  - `isDead`: Biến logic xác định trạng thái sống/chết của quái vật.
- **Các phương thức xử lý:**
  - `TakeDamage(float damage)`: Phương thức xử lý khi nhận sát thương từ người chơi, trừ máu quái vật, kích hoạt hiệu ứng chữ sát thương bay (Damage Text) và đẩy lùi vật lý.
  - `Die()`: Phương thức kích hoạt khi máu về 0, thực hiện tắt toàn bộ va chạm vật lý, chạy hiệu ứng tan biến và gọi sự kiện giải phóng bộ nhớ.
  - `UpdateSearchTarget()`: Chạy định kỳ để thực hiện quét va chạm tròn (OverlapCircle), xác định xem nhân vật chính có nằm trong tầm nhìn và góc quét hay không để cập nhật thuộc tính `target`.
  - `MovePatrol()`: Phương thức thực hiện di chuyển tuần tra qua lại quanh vị trí xuất phát ban đầu dựa trên một khoảng cách bán kính định sẵn.
  - `MoveReturnToOrigin()`: Ra lệnh cho quái vật quay trở lại vị trí đền thờ hoặc điểm tuần tra ban đầu khi người chơi di chuyển ra khỏi tầm truy đuổi.
  - `ApplyKnockback()`: Thực hiện tác dụng lực đẩy lùi vật lý lên Rigidbody2D của quái vật theo hướng ngược lại với nguồn gây sát thương.

*   **Hình 5.15: Biểu đồ lớp chi tiết của lớp GameSaveManager**
    ![save_manager_class](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/save_manager_class.png)

*Giải thích biểu đồ lớp GameSaveManager (Hình 5.15):*
Lớp `GameSaveManager` chịu trách nhiệm lưu trữ và phục hồi tiến trình chơi của game. Các thuộc tính và phương thức được đặc tả như sau:
- **Các thuộc tính thành viên:**
  - `instance`: Tham chiếu tĩnh duy nhất trỏ tới đối tượng quản lý đang hoạt động trong bộ nhớ (Singleton Pattern).
  - `saveFileName`: Chuỗi ký tự định nghĩa tên tệp lưu trữ mặc định trên thiết bị.
  - `activeSlotIndex`: Chỉ số ô lưu trữ đang được nạp để chơi game.
  - `currencyWallet`: Tham chiếu đến ScriptableObject quản lý số tiền vàng hiện có.
  - `inventoryState`: Tham chiếu đến ScriptableObject quản lý các vật phẩm đang nằm trong túi đồ.
  - `facilityManager`: Tham chiếu đến component quản lý mức nâng cấp của làng.
- **Các phương thức xử lý:**
  - `SaveActiveSlot()`: Phương thức tự động gọi lưu tiến trình chơi của ô nhớ hiện tại xuống tệp tin tương ứng.
  - `SaveGameToPath()`: Chuyển đổi toàn bộ cấu trúc dữ liệu sang chuỗi JSON và thực hiện ghi xuống ổ cứng.
  - `LoadGameFromSlot(int slotIndex)`: Đọc tệp tin JSON tương ứng với ô nhớ được chọn, chuyển đổi ngược sang đối tượng dữ liệu và phân phối dữ liệu cho các phân hệ.
  - `CreateNewGameSlot(int slotIndex)`: Tạo ra một ô lưu trữ trống mới với các giá trị chỉ số và thuộc tính mặc định ban đầu.
  - `CaptureSaveData()`: Phương thức thu thập các chỉ số thực tế từ các ScriptableObjects và các Manager hiện hành, nạp vào cấu trúc trung gian `GameSaveData` để chuẩn bị serialize.
  - `ApplySaveData()`: Lấy dữ liệu từ đối tượng `GameSaveData` đã giải tuần tự hóa để nạp ngược lại các phân hệ trong hệ thống khi tải game.

Để minh họa luồng truyền thông điệp giữa các đối tượng trong các tình huống tương tác thực tế, hệ thống đặc tả 2 use case cốt lõi thông qua biểu đồ trình tự:
1.  **Use Case Chuyển đổi linh hồn (Form Switch):** Sơ đồ trình tự tương tác giữa `PlayerInput`, `PlayerController`, `PlayerFormManager`, và `PlayerStats` khi thực hiện thay đổi hình dạng nhân vật được biểu diễn chi tiết tại Hình 5.16.
2.  **Use Case Lưu trữ tiến trình game (Save Game):** Khi người chơi tương tác cầu nguyện tại đền thờ hồi sinh (`RespawnShrine`), một yêu cầu lưu game được gửi tới `GameSaveManager`. Tiến trình tương tác truyền tin cụ thể được mô tả chi tiết tại sơ đồ trình tự ở Hình 5.11 (phần thiết kế kiến trúc hệ thống lưu trữ).

*   **Hình 5.16: Sơ đồ trình tự quá trình đồng bộ hóa chuyển đổi hình thái (Form Switch Sequence)**
    ![form_switch_sequence](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/FormSwitchSequence.png)

*Giải thích Sơ đồ trình tự chuyển đổi hình thái linh hồn (Hình 5.16):*
Biểu đồ mô tả chi tiết chuỗi tương tác thời gian thực giữa các thành phần con của thực thể người chơi khi thực hiện cơ chế biến hình (Form Switch):
1. Người chơi nhấn phím tương tác biến hình (`Tab` hoặc phím gán). Thành phần `PlayerInput` nhận diện tín hiệu và gửi thông báo sự kiện nhập liệu đến bộ điều phối trung tâm `PlayerController`.
2. `PlayerController` đóng vai trò là Mediator kiểm tra điều kiện an toàn (không bị khống chế, không đang rơi tự do) và gọi phương thức `SwitchForm()` đến `PlayerFormManager`.
3. `PlayerFormManager` thực hiện truy xuất thông tin cấu hình hình thái linh hồn mới từ tài sản tĩnh ScriptableObject `PlayerFormDataSO`.
4. `PlayerFormManager` tiến hành đồng bộ hóa các chỉ số máu tối đa, thể lực tối đa mới vào component quản lý thuộc tính `PlayerStats`.
5. `PlayerFormManager` thực hiện hủy đối tượng Prefab hình thể cũ và khởi tạo động (`Instantiate`) Prefab của hình thái linh hồn mới làm con của Player Root.
6. Lớp điều phối `PlayerController` gọi hàm `SyncPlayerMovementCollider()` đến component `PlayerMovement` để gán lại kích thước Collider vật lý và các điểm GroundCheck/WallCheck khớp chính xác với hình dạng mới, ngăn ngừa hiện tượng lỗi kẹt địa hình. Đồng thời, Animator của hình thái mới được rebind vào hệ thống hoạt ảnh để chạy mượt mà chuỗi chuyển động tương ứng.

---

### 5.3.2 Thiết kế cơ sở dữ liệu
Vì trò chơi được thiết kế như một phần mềm độc lập chạy ngoại tuyến hoàn toàn trên PC, hệ thống không sử dụng các hệ quản trị cơ sở dữ liệu quan hệ máy chủ (như SQL Server hay MySQL) để tránh đòi hỏi kết nối mạng và các chi phí cài đặt phức tạp cho người chơi.

Thay vào đó, trò chơi áp dụng thiết kế cơ sở dữ liệu hướng tài liệu cục bộ (document-based local file database) bằng định dạng JSON bền vững. Cơ sở dữ liệu được tổ chức dưới dạng một lớp tổng hợp gốc là `GameSaveData` chứa các tập hợp dữ liệu con đại diện cho trạng thái của từng phân hệ. Việc tổ chức cơ sở dữ liệu dưới dạng JSON giúp đảm bảo tốc độ đọc ghi cực nhanh, dung lượng lưu trữ nhẹ (dưới 100KB mỗi slot lưu trữ) và dễ dàng sao lưu, phục hồi thông qua các thư viện hệ thống tệp chuẩn của hệ điều hành.

Hệ thống quản lý lưu trữ được phân tách thành ba dạng độc lập: lưu trữ cấu hình âm thanh hệ thống bằng cơ chế `PlayerPrefs` của Unity, lưu trữ thiết lập phím bấm (Keybinds) người dùng bằng định dạng JSON độc lập, và lưu trữ tiến trình chơi game bằng định dạng JSON bền vững. Các thuộc tính và thành phần dữ liệu được quy định chi tiết bằng cấu trúc danh sách đặc tả dưới đây:

**Lưu trữ cấu hình hệ thống (qua PlayerPrefs):**
Các thiết lập âm lượng hệ thống cơ bản được ghi nhận cục bộ dưới dạng cặp khóa - giá trị bao gồm:
- `Pref_MasterVolume` (kiểu dữ liệu `float`, giá trị mặc định `1.0f`): Âm lượng tổng của toàn bộ trò chơi (Master Volume).
- `Pref_BgmVolume` (kiểu dữ liệu `float`, giá trị mặc định `0.8f`): Âm lượng của phần âm nhạc nền (BGM Volume).
- `Pref_SfxVolume` (kiểu dữ liệu `float`, giá trị mặc định `0.8f`): Âm lượng của phần hiệu ứng âm thanh chiến đấu (SFX Volume).

**Lưu trữ thiết lập phím bấm (Keybinds - qua input_profile.json):**
Các phím điều khiển do người dùng tùy biến được lưu tại đường dẫn thư mục tài liệu cá nhân (`MyDocuments/DreamKnight/input_profile.json`) dưới cấu trúc lớp `InputBindingProfile` chứa danh sách các phần tử `InputBindingEntry` bao gồm:
- `action` (kiểu dữ liệu `string`): Tên hành động có thể gán phím, ví dụ: `MoveUp`, `MoveDown`, `MoveLeft`, `MoveRight`, `Jump`, `Dodge`, `NormalAttack`, `Interact`, `UsePotion`, `UseTool`, `UseSpell`, `Transform`.
- `controlPath` (kiểu dữ liệu `string`): Đường dẫn phím bấm của New Input System tương ứng được gán cho hành động đó, ví dụ: `<Keyboard>/w`, `<Keyboard>/space`, `<Mouse>/leftButton`.

**Lưu trữ tiến trình chơi game chi tiết (qua GameSaveData):**
Dữ liệu chi tiết về tiến trình chơi của người chơi được lưu trong lớp cấu trúc `GameSaveData` bao gồm các trường:
- `gold` (kiểu dữ liệu `int`): Số lượng tiền vàng tích lũy hiện có của nhân vật chính.
- `inventory` (kiểu dữ liệu `InventorySaveData`): Danh sách lưu trữ thông tin các vật phẩm thu thập được trong túi đồ.
- `toolEquip` (kiểu dữ liệu `EquipmentSaveData`): Dữ liệu về trang bị công cụ (Tool) hiện tại của người chơi.
- `healingPotionEquip` (kiểu dữ liệu `EquipmentSaveData`): Dữ liệu về trang bị bình thuốc hồi máu hiện tại của người chơi.
- `shop` (kiểu dữ liệu `ShopSaveData`): Trạng thái đã mua hoặc chưa mua các vật phẩm đặc trưng trong cửa hàng NPC.
- `skillProgress` (kiểu dữ liệu `SkillProgressSaveData`): Dữ liệu theo dõi trạng thái mở khóa các kỹ năng phép thuật đã học.
- `facilityProgress` (kiểu dữ liệu `FacilityProgressSaveData`): Tiến trình nâng cấp vĩnh viễn các cơ sở thuộc tính cơ bản của nhân vật tại làng.
- `skillTreeProgress` (kiểu dữ liệu `SkillTreeProgressSaveData`): Tiến trình mở khóa các nhánh kỹ năng chiến đấu trong cây kỹ năng (Skill Tree).
- `portals` (kiểu dữ liệu `PortalSaveData`): Danh sách trạng thái kích hoạt của các cổng dịch chuyển nhanh (Portals).
- `doors` (kiểu dữ liệu `DoorSaveData`): Trạng thái đóng/mở của các cánh cửa lớn phân vùng bản đồ trong game.
- `worldPickups` (kiểu dữ liệu `WorldPickupSaveData`): Trạng thái ghi nhận các vật phẩm đã được nhặt trong màn chơi.
- `bossDefeats` (kiểu dữ liệu `BossDefeatSaveData`): Trạng thái đánh bại các Boss chính (đã tiêu diệt/chưa tiêu diệt) phục vụ cốt truyện.
- `equippedSpellId` (kiểu dữ liệu `string`): Định danh ID của kỹ năng phép thuật đang được người chơi chọn để thi triển.
- `slotIndex` (kiểu dữ liệu `int`): Chỉ mục ô nhớ lưu trữ game (Save Slot Index) đang được thực hiện.
- `playTimeSeconds` (kiểu dữ liệu `float`): Tổng thời gian chơi game đã ghi nhận thực tế (tính bằng giây).

**Phân loại các đối tượng ScriptableObjects trong kiến trúc game:**
Để hỗ trợ kiến trúc dữ liệu hướng cấu hình (Data-Driven Design), hệ thống phân tách ScriptableObjects thành hai nhóm riêng biệt:
- **Nhóm dữ liệu cấu hình tĩnh (Static Configuration Assets):**
  - `ItemDatabaseSO` (kế thừa từ `ScriptableObject`): Chứa danh sách định nghĩa thông số tĩnh của toàn bộ các vật phẩm trong trò chơi.
  - `SpellDatabaseSO` (kế thừa từ `ScriptableObject`): Chứa danh sách định nghĩa thông số cơ bản của các kỹ năng phép thuật có thể học.
  - `PlayerFormDataSO` (kế thừa từ `ScriptableObject`): Lưu trữ cấu hình prefab, icon HUD, lượng máu tối đa mặc định và Animator Controller của các hình thái linh hồn.
- **Nhóm lưu trữ trạng thái chạy runtime (Runtime State Assets):**
  - `InventoryStateSO` (kế thừa từ `ScriptableObject`): Quản lý danh sách các vật phẩm thực tế đang tồn tại trong túi đồ của người chơi.
  - `CurrencyWalletSO` (kế thừa từ `ScriptableObject`): Quản lý số dư tiền vàng hiện thời và thực hiện các giao dịch cộng/trừ tiền trực tiếp.
  - `ShopStateSO` (kế thừa từ `ScriptableObject`): Theo dõi và cập nhật trạng thái mua sắm vật phẩm và mở khóa kỹ năng của cửa hàng.

---

## 5.4 Xây dựng ứng dụng

### 5.4.1 Thư viện và công cụ sử dụng
Quá trình xây dựng và phát triển trò chơi "Dream Witch" sử dụng các công cụ lập trình, thư viện bên thứ ba và phần mềm đồ họa với thông tin chi tiết được thống kê trong Bảng 5.2.

#### Bảng 5.2: Danh sách các thư viện và công cụ phát triển trò chơi
| Mục đích sử dụng | Công cụ / Thư viện (Phiên bản) | Địa chỉ URL chính thức |
| :--- | :--- | :--- |
| Engine phát triển game | Unity Editor (6000.0.40f1 LTS) | [https://unity.com/](https://unity.com/) |
| Trình soạn thảo mã nguồn | Visual Studio Code (v1.90+) | [https://code.visualstudio.com/](https://code.visualstudio.com/) |
| Trợ lý lập trình AI | Antigravity (DeepMind Agent) | N/A (Tích hợp trong môi trường phát triển) |
| Hệ thống hội thoại | Yarn Spinner for Unity (v2.3.0) | [https://yarnspinner.dev/](https://yarnspinner.dev/) |
| Diễn họa Boss 2D | Spine 2D Runtime (v4.2) | [https://esotericsoftware.com/](https://esotericsoftware.com/) |
| Giải thuật tìm đường | A* Pathfinding Project (v4.2.17) | [https://arongranberg.com/astar/](https://arongranberg.com/astar/) |
| Quản lý phiên bản | Git & GitHub Desktop | [https://github.com/](https://github.com/) |
| Thiết kế đồ họa và Sprite | Adobe Photoshop & Aseprite | [https://www.aseprite.org/](https://www.aseprite.org/) |

### 5.4.2 Kết quả đạt được
Sản phẩm sau quá trình phát triển là bản đóng gói trò chơi hoàn chỉnh hoạt động độc lập trên hệ điều hành Windows 64-bit (PC Standalone). Bản đóng gói bao gồm tệp thực thi `DreamWitch.exe` cùng thư mục tài nguyên `DreamWitch_Data`. Mã nguồn C# của dự án được tổ chức chặt chẽ theo cấu trúc phân rã thành các gói chức năng (packages) bao gồm: `Player`, `Enemy`, `System`, `UI`, `Interfaces`, `Core`, `Trap`, và `VFX`.

Để minh chứng cho quy mô phát triển của dự án, các thông tin định lượng chi tiết về mã nguồn C# và dung lượng các thành phần liên quan được tổng hợp trong Bảng 5.3.

#### Bảng 5.3: Thống kê định lượng mã nguồn và tài nguyên dự án
| Chỉ số định lượng | Giá trị ghi nhận | Đơn vị đo lường |
| :--- | :--- | :--- |
| Số lượng tệp tin mã nguồn C# (`.cs`) | 337 | Tệp tin |
| Tổng số dòng mã nguồn (LOC) | 46.490 | Dòng code |
| Số lượng gói phân hệ chức năng chính | 8 | Gói (Namespace) |
| Dung lượng thư mục mã nguồn C# thô | 1,54 | Megabytes (MB) |
| Dung lượng thư mục tài nguyên dự án (Assets) | 1,82 | Gigabytes (GB) |
| Dung lượng bản đóng gói sản phẩm (.zip) | ~350 | Megabytes (MB) |

---

## 5.5 Sản phẩm
Để đánh giá trực quan kết quả lập trình và thiết kế cơ chế, phần này trình bày diễn tiến vận hành thực tế của trò chơi "Dream Witch" theo một kịch bản chơi game (gameplay scenario) hoàn chỉnh đi qua các phân khu bản đồ từ lúc khởi động cho tới khi hoàn thành nhiệm vụ cốt lõi. Người đọc có thể hình dung toàn bộ trải nghiệm mà không cần trực tiếp vận hành sản phẩm.

Kịch bản trải nghiệm gồm 5 giai đoạn chính như sau:

### Giai đoạn 1: Khởi động và thiết lập ban đầu (Title Screen & Save Slots)
Khi người chơi kích hoạt tệp `DreamWitch.exe`, hệ thống hiển thị màn hình bắt đầu với âm nhạc u sầu đặc trưng (Hình 5.17). Giao diện cung cấp các tùy chọn để bắt đầu chơi mới hoặc tiếp tục tiến trình cũ. Khi chọn "Continue", giao diện Save Slot hiển thị thông tin chi tiết về từng ô lưu trữ (thời gian chơi, lượng vàng tích lũy, các chỉ số cơ bản) giúp người chơi quản lý tiến trình (Hình 5.18).

*   **Hình 5.17: Màn hình khởi động trò chơi (Title Screen)**
    ![TitleScreen](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/TitleScreen.jpg)
*   **Hình 5.18: Giao diện chọn Slot tiếp tục chơi (Save Slot Menu)**
    ![MainMenu](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/MainMenu.jpg)

### Giai đoạn 2: Thám hiểm khu vực Di tích và điều khiển vật lý (Ruin Exploration)
Nhân vật phù thủy xuất hiện tại điểm xuất phát an toàn là Nhà thờ (Church), sau đó di chuyển qua cửa lớn sang phân khu Di tích (Ruin). Người chơi thực hiện kiểm soát chuyển động nhân vật: chạy di chuyển mượt mà trên nền tảng, thực hiện nhảy đơn, nhảy đôi để vượt qua các vực thẳm và bám vách leo tường (Wall Climb). Hệ thống vật lý nhận diện chính xác các va chạm với gạch đá cổ và tiêu hao thể lực (Stamina) tương ứng khi bám tường leo lên. Giao diện hiển thị rõ thanh HUD lượng máu/thể lực ở góc trên bên trái (Hình 5.19 và Hình 5.20).

*   **Hình 5.19: Thanh trạng thái HUD (Máu/Thể lực/Mana)**
    ![HUD](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/HUD.png)
*   **Hình 5.20: Platforming và thám hiểm tại khu vực Di tích (Ruin)**
    ![Level](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/Level.png)

### Giai đoạn 3: Cơ chế chiến đấu cận chiến và né tránh (Combat & Dash/Dodge)
Trong quá trình thám hiểm Ruin, nhân vật đối đầu với quái vật thường đầu tiên tuần tra trên đường (BoneDog - quái thú xương). Người chơi thi triển chuỗi đòn đánh cận chiến liên hoàn 3-hit combo tạo ra các tia sáng cắt chém vật lý. Khi quái thú lao tới tấn công, người chơi kích hoạt phím lướt (Dash) để né tránh đòn đánh ngay trước thời điểm bị va chạm. Một cú né tránh chuẩn xác giúp nhân vật không nhận sát thương, đồng thời người chơi có thể lập tức quay lại phản công cận chiến để tiêu diệt quái vật (Hình 5.21).

*   **Hình 5.21: Nhân vật cận chiến chém quái thú thường kèm chữ hiển thị sát thương bay**
    ![QuaiVatThuong](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/QuaiVatThuong.png)

### Giai đoạn 4: Biến dạng thực thể và phép thuật tầm xa (Form Switch & Spellcasting)
Tại các điểm đánh dấu đặc biệt trong di tích, người chơi tương tác với các xác cổ của sinh vật hắc ám để hấp thụ sức mạnh và mở khóa cơ chế biến dạng (Form Switch). Khi nhấn phím chuyển dạng, nhân vật phù thủy chuyển đổi ngoại hình (Animator Controller thay đổi lập tức để đổi bộ hoạt ảnh và thay đổi Sprite vật lý tương ứng của form mới) sang hình thái quái vật lửa, cho phép thi triển các phép thuật tầm xa mạnh mẽ vượt qua các dòng sông axit hiểm trở dẫn sang phân khu Núi lửa (Volcano) (Hình 5.22 và Hình 5.23).

*   **Hình 5.22: Interact Prompt hiện lên khi đứng gần xác quái vật để biến hình**
    ![BienHinh](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/BienHinh.png)
*   **Hình 5.23: Thi triển đạn phép thuật tầm xa được quản lý bởi Object Pooling**
    ![SpellGameplay](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/SpellGameplay.png)

### Giai đoạn 5: Đối đầu Boss Golem và lưu trữ tiến trình (Boss Fight & Progress Saving)
Cuối khu vực Di tích, người chơi tiến vào phòng Boss Golem khổng lồ. Trận đấu diễn ra căng thẳng khi Boss có lượng máu lớn (hiển thị bằng thanh máu dài dưới đáy màn hình) và sử dụng AI tìm đường A* săn đuổi người chơi kết hợp đập tay tạo sóng chấn động. Sau khi tiêu diệt Boss thành công bằng sự kết hợp giữa biến dạng và né tránh, người chơi di chuyển tới Đền thờ hồi sinh (Shrine), thực hiện cầu nguyện để kích hoạt hệ thống tự động lưu trữ ghi lại toàn bộ tiến trình chơi thành tệp tin JSON lưu trữ trên thiết bị (Hình 5.24 và Hình 5.25).

*   **Hình 5.24: Đối đầu với Boss khổng lồ Golem**
    ![BossTranChien](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/BossTranChien.png)
*   **Hình 5.25: Boss thi triển chiêu đập đất tạo chấn động**
    ![BossTranChien2](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/BossTranChien2.png)

### Các bảng giao diện chức năng quan trọng khác:
Để người đọc có thể hình dung toàn diện về chiều sâu của hệ thống nhập vai và các tính năng tương tác phụ trợ trong "Dream Witch", các giao diện quản lý chỉ số nhân vật, cửa hàng mua sắm vật phẩm, cây kỹ năng và bản đồ định vị thế giới được trình bày chi tiết:

*   **Hình 5.26: Bảng trạng thái (Status Panel) hiển thị chỉ số chi tiết các linh hồn**
    ![StatusUI](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/StatusUI.jpg)
*   **Hình 5.27: Giao diện Cửa hàng (Shopkeeper UI) mua sắm các phép thuật và công cụ**
    ![ShopUI](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/ShopUI.jpg)
*   **Hình 5.28: Nhánh kỹ năng (Skill Tree) mở khóa các đòn đánh nâng cao**
    ![SkillTreeUI](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/SkillTreeUI.jpg)
*   **Hình 5.29: Bản đồ thế giới (Full Map UI) hỗ trợ người chơi định vị**
    ![FullMapUI](file:///d:/du%20an/Doan_DreamKnight/Assets/Project/Script/QuyenDoan/QuyenDoan/Hinhve/FullMapUI.jpg)

---

## 5.6 Kiểm thử
Ca kiểm thử phần mềm được tiến hành bằng phương pháp kiểm thử hộp đen (Black-box Testing) tập trung kiểm tra độ chính xác của các chức năng cốt lõi và tính ổn định của cơ chế tương tác vật lý trực quan trong game.

Chi tiết thiết kế các ca kiểm thử cho ba phân hệ chức năng quan trọng nhất được trình bày trong Bảng 5.4.

### Bảng 5.4: Bảng đặc tả các trường hợp kiểm thử cốt lõi
| Mã ca | Phân hệ / Chức năng | Các bước thực hiện | Kết quả kỳ vọng (Đạt) |
| :--- | :--- | :--- | :--- |
| **TC-01** | Di chuyển cơ bản | Nhấn giữ phím `A/D` trên bàn phím. | Nhân vật tăng tốc di chuyển mượt mà sang trái/phải, đổi hướng ngay lập tức và chạy animation chạy tương ứng. |
| **TC-02** | Nhảy đôi (Double Jump) | Nhấn phím `Space` lần một, tiếp tục nhấn `Space` khi nhân vật ở trên không. | Thực hiện cú nhảy thứ hai tạo lực đẩy dọc đi lên, tiêu hao 10 Stamina, chạy animation nhảy đôi. |
| **TC-03** | Leo tường (Wall Climb) | Nhấn phím `W` kết hợp giữ phím hướng `A/D` bám sát vào tường thẳng đứng. | Nhân vật bám chặt vách tường, di chuyển đi lên đều đặn, tiêu hao Stamina liên tục. Khi nhả `A/D` hoặc hết Stamina nhân vật tự động rơi xuống. |
| **TC-04** | Kích hoạt biến hình | Di chuyển tới xác cổ thực thể và nhấn tương tác để hấp thụ, mở khóa Form Switch. | Hệ thống kích hoạt khóa kỹ năng, hiển thị thông báo mở khóa dạng chuyển đổi mới thành công. |
| **TC-05** | Thực thi Form Switch | Nhấn phím chuyển dạng (`Tab`) khi đang đứng yên hoặc di chuyển. | Nhân vật đổi cấu hình Animator và Sprite lập tức, các thuộc tính (Max HP, tốc độ chạy) được cập nhật lại theo cấu hình của dạng mới. |
| **TC-06** | Lưu game tại đền thờ | Di chuyển nhân vật đến gần Đền thờ hồi sinh và nhấn phím tương tác để lưu dữ liệu. | Hệ thống ghi nhận trạng thái game, xuất ra tệp `save_slot_0.json` lưu trữ chính xác vị trí, chỉ số vàng và tiến trình nâng cấp kỹ năng. |
| **TC-07** | Tải lại tiến trình | Khởi chạy lại game, chọn "Continue" và chọn slot lưu trữ tương ứng. | Đọc thành công tệp tin JSON, khôi phục nhân vật xuất hiện đúng vị trí Đền thờ đã lưu với đầy đủ lượng vàng và thuộc tính. |

### 5.6.1 Tổng kết kết quả kiểm thử
Sau quá trình thực thi kiểm thử hệ thống với 7 ca kiểm thử cốt lõi trên nhiều cấu hình thiết bị khác nhau, kết quả kiểm tra được tổng hợp chi tiết trong Bảng 5.5.

#### Bảng 5.5: Bảng tổng hợp kết quả kiểm thử hệ thống
| Nhóm kiểm thử | Tổng số ca | Số ca đạt | Số ca lỗi | Tỷ lệ thành công |
| :--- | :--- | :--- | :--- | :--- |
| **Di chuyển & Leo tường** | 3 | 3 | 0 | 100% |
| **Chuyển đổi dạng (Form Switch)** | 2 | 2 | 0 | 100% |
| **Lưu trữ & Tải file JSON** | 2 | 2 | 0 | 100% |
| **Tổng cộng** | **7** | **7** | **0** | **100%** |

Kết quả kiểm thử cho thấy toàn bộ các chức năng then chốt của trò chơi hoạt động ổn định, chính xác theo đúng đặc tả thiết kế hệ thống. Hệ thống máy trạng thái nhân vật phản hồi tốt các tương tác điều khiển, cơ chế chuyển đổi hình thái cập nhật đồng bộ các chỉ số runtime, và dữ liệu người chơi được tuần tự hóa thành công sang định dạng JSON mà không gây ra bất kỳ hiện tượng rò rỉ dữ liệu hoặc xung đột logic nào.

---

## 5.7 Triển khai
Trò chơi được thiết kế và triển khai như một ứng dụng độc lập chạy ngoại tuyến (PC Standalone) trên hệ điều hành Windows:
- **Đóng gói sản phẩm (Build & Packaging):** Trò chơi sử dụng bộ đóng gói tích hợp của Unity (Unity Build Pipeline) để biên dịch sang mã máy gốc chạy trên nền tảng Windows 64-bit. Kết quả đóng gói bao gồm tệp thực thi chính `DreamWitch.exe` và thư mục dữ liệu tài nguyên đã được biên dịch `DreamWitch_Data`.
- **Phương thức phân phối (Distribution):** Bản đóng gói được nén lại dưới dạng tệp tin nén (`.zip` hoặc `.rar`) để phân phối trực tiếp cho người chơi thông qua dịch vụ lưu trữ đám mây (như Google Drive) hoặc trang web lưu trữ trò chơi độc lập (như itch.io). Người sử dụng chỉ cần tải về, giải nén và kích hoạt trực tiếp tệp `DreamWitch.exe` để trải nghiệm mà không cần cài đặt thêm.
- **Thiết lập cấu hình vận hành (Configuration):** Trò chơi mặc định kích hoạt tính năng đồng bộ dọc (V-Sync) để đồng bộ tần số quét màn hình (khóa khung hình ở mức 60 FPS hoặc tần số quét tối đa của màn hình như 144Hz), giảm thiểu xé hình và ổn định tài nguyên CPU/GPU. Độ phân giải màn hình hỗ trợ tự động thích ứng với tỉ lệ 16:9 phổ biến, từ HD ($1280 \times 720$) đến Full HD ($1920 \times 1080$), 2K và 4K thông qua component `Canvas Scaler`.
