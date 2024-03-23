/*! @mainpage ProjectKJ LoginServer Guide
* ProjectKJ LoginServer 관련 문서입니다.\n
* 왼쪽의 목차를 보고 관심있는 메서드 설명을 참조해주세요.\n
* @section ProjectKJ Diagram ProjectKJ 다이어그램
* ProjectKJ의 다이어그램입니다.
* @image html ProjectKJDiagram.jpg "ProjectKJ Diagram"
*/



namespace LoginServer
{
    /// <summary>
    /// Program 클래스 입니다.
    /// 윈도우폼이 자동으로 생성해주는 코드이며 수정은 자제해주세요.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        ///  메인 함수입니다
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new LoginServer());
        }
    }
}