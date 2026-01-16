# 🚜 Colony Sim Prototype (RimWorld Style)

Unity 6 (6000.3.3f1)를 활용해 개발 중인 림월드 스타일의 콜로니 시뮬레이션 게임입니다.
다수의 AI 에이전트(로봇)가 자율적으로 작업을 분배받고 수행하는 작업 시스템(Job System) 구조 설계에 집중했습니다.

## 🛠 개발 환경
* Engine: Unity 6000.3.3f1
* Language: C#
* IDE: Visual Studio 2022

## 💻 핵심 구현 내용

### 1. 중앙 집중식 작업 분배 시스템 (Job Dispatcher)
다수의 유닛이 충돌 없이 효율적으로 일하도록 생산자-소비자 패턴을 응용해 구현했습니다.
* [JobDispatcher.cs](Assets/Scripts/Jobs/JobDispatcher.cs): 게임 내 모든 일감을 관리하는 매니저 클래스입니다.
    * 단순 `Queue`가 아닌 `List`를 사용하여 작업의 중간 취소 및 우선순위 재조정이 가능하도록 설계했습니다.
    * Race Condition 방지: `HasActiveJob` 메서드를 통해 하나의 작업물(건설, 채집 등)에 여러 로봇이 동시에 할당되지 않도록 중복 검사를 수행합니다.
    * 거리 계산 최적화: 그리드 기반 게임 특성에 맞춰 유클리드 거리 대신 맨해튼 거리(Manhattan Distance) 공식을 사용하여 연산 비용을 줄였습니다.

### 2. 에이전트 FSM 및 비동기 행동 처리
* [RobotAgent.cs](Assets/Scripts/Jobs/RobotAgent.cs): 개별 로봇의 AI 로직입니다.
    * 상태 패턴(State Pattern): 로봇의 상태를 `Idle`, `Moving`, `Working` 등으로 나누어 관리합니다.
    * Coroutine 활용: 건설, 채집, 운반 등 시간이 소요되는 작업을 코루틴으로 처리하여 메인 스레드 블로킹을 방지하고 자연스러운 행동 흐름을 구현했습니다.
    * 자원 반환 및 운반 로직: 작업 완료 후 결과물을 가장 가까운 창고(`StorageBox`)로 운반하거나 발전기에 연료를 공급하는 연계 작업을 자동으로 생성합니다.

### 3. 데이터 주도 설계 (Data-Driven)
* `ScriptableObject`를 적극 활용하여 아이템, 식물, 제작 레시피 등의 데이터를 코드 수정 없이 인스펙터에서 관리할 수 있도록 구조화했습니다.
