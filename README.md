
# BIO - :ru:

Я работаю в Санкт-Петербургском государственном университете телекоммуникаций им. проф. М. А. Бонч-Бруевича на каф. Сетей Связи и Передачи Данных (ССиПД) и в лаборатории "Исследование сетевых технологий с ультра малой задержкой и сверхвысокой плотностью на основе широкого применения искусственного интеллекта для сетей 6G» (MEGANETLAB 6G) в рамках мегагранта Минобрнауки.

Команда, частью которой я являюсь, занимается исследованиями в сфере [HTC](https://www.researchgate.net/publication/366122391_Challenges_in_Implementing_Low-Latency_Holographic-Type_Communication_Systems) (Holographic Type Vommunication) и [Metaverse](https://www.itu.int/en/ITU-T/focusgroups/mv/Pages/default.aspx), я единственный программист команды, поэтому планирование архитектуры, прототипирование, разработка, тестирование, поддержка кода и прочие подобные задачи выполняю я.

Весь проект MVP был придуман, разработан и научно обоснован лично мной. 

# Multiple Virtual Professors - :ru:

**MVP** - система приложений, имеющая клиент-серверную архитектуру для обеспечения услуг конференц-связи без использования видео-потока.
Все известные мне приложения конференц-связи для подобных целей используют **WebRTC** или подобные способы передачи видео и аудио потоков между пользователями. Разрабатываемая система использует другой подход и нацелена на использование в ["Сетях 2030"](https://www.itu.int/dms_pubrec/itu-r/rec/m/R-REC-M.2160-0-202311-I%21%21PDF-E.pdf). Вместо видеопотока **MVP** передает 3D-модель собеседника и координаты контрольных точек с тела человека, снятые при помощи сенсора глубины **Kinect v2**.

На данный момент ведется разработка системы **MVP_2.0**, для которой написано собственное сетевое решение на языке C#, тогда как [MVP_1.0](https://github.com/Barlogov/MVP) был рабочим прототипом для Бакалаврской квалификационной работы, который использовал множество "быстрых и не самых практичных" решений по типу технологии [Mirror](https://mirror-networking.com/) 

## Система приложений стремится быть кроссплатформенной и имеет следующие составляющие (все подпроекты находятся в разработке):
- [Серверная](https://github.com/Barlogov/MVP_2_0_Server) для Linux __(полный MVS проект)__
- [Серверная](https://github.com/Barlogov/MVP_2_0_Server) для Windows __(полный MVS проект)__
- [Клиентская](https://github.com/Barlogov/MVP_2.0_PC_FullBody) для Windows с обычным(и) монитором(и) и полем зрения сенсора в полный рост пользователя __(полный Unity проект)__
- [Клиентская](https://github.com/Barlogov/MVP_2.0_PC_HalfBody) для Windows с голографическим октаэдром и полем зрения сенсора только на верхнюю часть человека __(/Assets/Scripts, только папка со скриптами, т.к. LFS не позволяет загружать более 1Гб больших файлов)__
- [Клиентская](https://github.com/Barlogov/MVP_2.0_PC_HoloTube) для Windows с голографическим цилиндром без поля зрения сенсора __(Unstable - т.к. прямо сейчас работаю над ним)(/Assets/Scripts, только папка со скриптами, т.к. LFS не позволяет загружать более 1Гб больших файлов)__
- [Клиентская](https://github.com/Barlogov/MVP_2.0_Hololens) для XR очков Hololens __(Разработка только началась, проект содержит только тестовые сцены)__
- [Клиентская](https://github.com/Barlogov/MVP) для AR приложения на Android/IOS __(Android build первой версии)__

## Система подразумевает межсетевое взаимодействие клиентов и сервера. 
- Для передачи "обязательного к доставке" трафика между клиентами и сервером, по типу авторизации, списка участников и их IP:PORT адресов, другой служебной информации используется собственный протокол **(Прикладной уровень модели OSI)**, написанный поверх **TCP**.
- Для передачи трафика "реального времени" так же используется собственный протокол **(Прикладной уровень модели OSI)**, но написанный поверх **UDP**, т.к. большую роль в данном случае играет скорость доставки, а не полнота информации.

## Сетевое взаимодействие реализовано следующим образом: 
- Имеется выделенный сервер c "белым" IP адресом на котором запущенно серверное приложение
- К серверу подключаются клиенты и проходят регистрацию/авторизацию (в разработке)
- После успешного подключения к серверу клиент может создать новую сессию 
- Сессия создается при помощи аналога RPC запроса от клиента к серверу
- Остальные клиенты могут подключиться к этой сессии при помощи кода доступа
- Сервер ведет учет всех клиентов во всех сессиях и при подключении новых или отключении существующих уведомляет об этом остальных участников сессии
- До этого все взаимодействия носили Клиент-Серверный характер по протоколу **TCP**, основной масса передаваемого трафика является дальнейшая передача **UDP** пакетов от клиента к клиенту

## Используемые технологии:
- using System.Threading;
- using System.Threading.Tasks; - асинхронность и многопоточность
____
- using System.Net;
- using System.Net.Sockets;
- using LumiSoft.Net.STUN.Client; - сетевая составляющая
____
- using Newtonsoft.Json; - сериализация
____
- using System.Linq; - Linq запросы
____
- using NAudio.Wave; - считывание и воспроизведение аудио потоков
____
- using Windows.Kinect;
- using Windows.Kinect.JointType; - библиотека Kinect для отслеживания людей в кадре и их ключевых точек
____
- Собственные протоколы поверх TCP и UDP
____
- 3D Render Engine: Unity
- UI / UX: Unity
____

## Фото-примеры работы системы:

### MVP_2.0_PC_FullBody (левый монитор) & Server (правый монитор):
![MVP_2 0_PC_FullBody_And_Server_Screenshot](https://github.com/user-attachments/assets/744feb69-cdf9-46de-baeb-8ef601514978)

### MVP_2.0_PC_HalfBody:
![MVP_2 0_PC_HalfBody_Screenshot](https://github.com/user-attachments/assets/9c19e1b1-0714-4a8a-80a6-ad0af08a5283)

### MVP_2.0_PC_HoloTube:
![MVP_2 0_PC_HoloTube_Screenshot](https://github.com/user-attachments/assets/09552bf8-b284-4959-b79a-bdb4c1ff6d94)

### MVP AR версия на Android:
![Screenrecorder-2023-10-12-20-19-42-817](https://github.com/user-attachments/assets/c8f3e704-9f2d-4f66-8229-3dbf2f3f868a)

## Новостные статьи, связанные с проектом:

- [5 канал](https://www.5-tv.ru/news/484939/rossijskie-ucenye-predstavili-proekt-sobstvennoj-metavselennoj-nakonkurse-oon/?utm_source=yxnews&utm_medium=desktop&utm_referrer=https%3A%2F%2Fdzen.ru%2Fnews%2Fsearch%3Ftext%3D)
- [SUT - ООН](https://www.sut.ru/bonchnews/science/14-05-2024-uchenie-spbgut-pobedili-v-konkurse-oon-s-proektom-golograficheskoy-vselennoy)
- [SUT - МАФ](https://www.sut.ru/bonchnews/public-life/07-03-2024-svyaz-na-severnom-poluse-i-robot-avatar-dlya-arktiki:-spbgut-na-molodezhnom-arkticheskom-forume)
- [РЕН ТВ](https://amp.ren.tv/news/v-rossii/1197085-vserossiiskii-molodezhnyi-arkticheskii-forum-startoval-v-kronshtadte)
- [SUT - ИТ-Диалог](https://www.sut.ru/bonchnews/industry/14-11-2023-spbgut-predstavil-peredovie-resheniya-i-ekspertizu-na-forume-it-dialog)

## Научные статьи, связанные с проектом:
- [ITT 2022](https://elibrary.ru/item.asp?id=50092898)
- [Конференция им. Попова 2022](https://elibrary.ru/item.asp?id=53913795)
- [Балтийский форум 2023](https://www.elibrary.ru/item.asp?id=64227900)
- [ICACNGC 2023](https://www.sut.ru/new_site/images/blocks/1696858755.pdf)
- [DCCN 2023](https://link.springer.com/chapter/10.1007/978-3-031-50482-2_3)
- [New2an 2023](https://new2an.info/NEW2AN-Final-2023.pdf)
