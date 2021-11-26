Torrent-Tracker-Server-By-.Net-Core-Based


bittorrent & webtorrent tracker server by C# .Net Core Based.

IPv4 Only, .Net Core 3.1, 


<BitTorrent Tracker(tcp, udp)>
[ http, udp ] protocol supported.

<WebTorrent Tracker(webRTC)>
[ websocket ] protocol supported.


트래커 서버로 가즈아

Windows 10, Linux Ubuntu 20.04 LTS Tested.

아직 충분한 테스트를 진행하진 않았으므로 버그가 있을수도 있음요.

추후 설정파일등을 통해서 port, threads, 접근가능 torrent client종류 지정등의 옵션 지정기능 추가,
웹을 통한 트래커 서버의 관리자 페이지등 추가 계획 있음.


일반 bittorrent용 트래커 프로토콜인 http, udp 추가,
webRTC 기반 webtorrent용 트래커 프로토콜인 websocket 추가.



ASP.Net Core API Template + https://github.com/Azure/DotNetty Used.

webtorrent Reference
https://github.com/webtorrent/bittorrent-tracker
(javascript => c#)
