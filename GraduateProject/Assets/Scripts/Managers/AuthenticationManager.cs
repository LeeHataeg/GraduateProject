using Firebase.Auth;
using Firebase.Extensions;
using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


// 입장할 때 한번 하는건데 굳이 Manager로 올려서 GameManager에 둬야함?
// 내가 봤을 땐 에반데
//  따라서 StartScene에서 특정 오브젝트에 달아두고 해당 씬에서만 굴러가도록
//      authUser가 필요하면 로그인 후 GameManager로 보내주던가 ㅇㅇ.
//  아니 근데 생각해보니 ???를 보장하려면 로그인 유지되는지를 체크해야하는거 아님?
// 그럼 결국 GameManager에 달아줘야겠네.ㅇㅇ
public class AuthenticationManager : MonoBehaviour
{
    [Header("로그인 UI")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField emailInputField;    // 공백 문자?(space) 제거 필요 ㅇㅇ
    [SerializeField] private TMP_InputField pwInputField;       // 위와 동일
    [SerializeField] private Button loginBtn;
    [SerializeField] private Button registerBtn;

    // 얘는 한 2.5f 떴다가 fade out 해야함. 그게 기분이가 조음,
    [SerializeField] private TextMeshProUGUI authStatus;    // 로그인 현황 (ex. 잘못된 pw입니다. 등)
    [SerializeField] private float statusTime = 2.5f;

    private FirebaseAuth auth;      // 인증용 변수??????
    public FirebaseUser authUser;

    [Header("게임 시작 관련")]
    [SerializeField] private GameObject StartPanel;

    private void Awake()
    {
        if(authUser == null)
            auth = FirebaseAuth.DefaultInstance;
        else
        {
            Debug.Log($"[Auth] 이미 로그인 상태: {authUser.Email}");

            if (loginPanel != null) loginPanel.SetActive(false);
            if (StartPanel != null) StartPanel.SetActive(true);

            // 로그인 버튼은 사실상 쓸 일 없으니 꺼버려도 됨(선택사항)
            SetBtnInteractable(false);
        }
    }

    private void Start()
    {
        // 앱/씬이 새로 켜졌을 때, 이미 로그인된 유저가 있는지 확인
        authUser = auth.CurrentUser;

        if (authUser != null)
        {
            Debug.Log($"[Auth] 이미 로그인 상태: {authUser.Email}");

            if (loginPanel != null) loginPanel.SetActive(false);
            if (StartPanel != null) StartPanel.SetActive(true);

            // 로그인 버튼은 사실상 쓸 일 없으니 꺼버려도 됨(선택사항)
            SetBtnInteractable(false);
        }
        else
        {
            Debug.Log("[Auth] 로그인 필요.");

            if (loginPanel != null) loginPanel.SetActive(true);
            if (StartPanel != null) StartPanel.SetActive(false);

            SetBtnInteractable(true);
        }
    }
    public void CreateAccount()
    {
        Debug.Log("Register 입장");


        SetBtnInteractable(false);

        string emailTxt = emailInputField.text.Trim();
        string pwTxt = pwInputField.text.Trim();

        if (string.IsNullOrEmpty(emailTxt) || string.IsNullOrEmpty(pwTxt))
        {
            StartCoroutine(popupAndFadeoutAuthStatus("이메일과 비밀번호를 입력하세요"));
            SetBtnInteractable(true);
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(emailTxt, pwTxt).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                StartCoroutine(popupAndFadeoutAuthStatus("회원가입 실패"));
                SetBtnInteractable(true);
                return;
            }
            if (task.IsCanceled)
            {
                StartCoroutine(popupAndFadeoutAuthStatus("회원가입 취소"));
                SetBtnInteractable(true);
                return;
            }

            authUser = task.Result.User;
            loginPanel.SetActive(false);
            StartPanel.SetActive(true);
        });
    }

    public void Login()
    {
        Debug.Log("Login 입장");

        SetBtnInteractable(false);

        string emailTxt = emailInputField.text.Trim();
        string pwTxt = pwInputField.text.Trim();

        if (string.IsNullOrEmpty(emailTxt) || string.IsNullOrEmpty(pwTxt))
        {
            StartCoroutine(popupAndFadeoutAuthStatus("이메일과 비밀번호를 입력하세요"));
            Debug.Log("오입력");

            SetBtnInteractable(true);
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(emailTxt, pwTxt).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                StartCoroutine(popupAndFadeoutAuthStatus("로그인 실패"));
                Debug.Log("Login 실패");

                SetBtnInteractable(true);

                return;
            }
            if (task.IsCanceled)
            {
                StartCoroutine(popupAndFadeoutAuthStatus("로그인 취소"));
                Debug.Log("Login 취소");

                SetBtnInteractable(true);
                return;
            }
            Debug.Log("해치웠나?");


            authUser = task.Result.User;
            loginPanel.SetActive(false);
            StartPanel.SetActive(true);

        });
    }

    // 굳이 로그아웃 기능 구현 안할 듯?
    // 구현하려면 esc 등으로 Home으로 나가기 구현해야하는데 시간 없엉.
    public void Logout()
    {
        auth.SignOut();
        loginPanel.SetActive(true);
        StartPanel.SetActive(false);
        SetBtnInteractable(true);
     }
    private IEnumerator popupAndFadeoutAuthStatus(string status)
    {
        if (authStatus == null)
            yield break;

        Color c = authStatus.color;
        c.a = 1f;
        authStatus.color = c;
        authStatus.text = status;

        authStatus.gameObject.SetActive(true);

        // Popup
        authStatus.text = status;
        yield return new WaitForSeconds(statusTime);

        // fade out

        float duration = 1.5f;
        float deltatTime = 0f;

        while(deltatTime < duration)
        {
            deltatTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, deltatTime / duration);
            c.a = alpha;
            authStatus.color = c;
            yield return null;
        }

        c.a = 0f;
        authStatus.color = c;
        authStatus.gameObject.SetActive(false);
    }

    private void SetBtnInteractable(bool interactable)
    {
        if(loginBtn != null)
            loginBtn.interactable = interactable;

        if(registerBtn != null)
            registerBtn.interactable = interactable;
    }
}