using System;
using System.Collections;
using UnityEngine;

public class AuthScreenController : MonoBehaviour
{
	public ConfirmPopupController GeneralPopup;

	public UIButtonTween GeneralPopupShowTween;

	public UIButtonTween AgeGateShowTween;

	public string NextSceneName = "AssetLoader";

	public BusyIconController busyIconController;

	private bool isComplete;

	public static bool AuthStarted;

	private static bool staticInited;

	private void SocialLogin()
	{
		PlayerPrefs.DeleteKey("RetrySocialLogin");
		if (SocialManager.Instance.IsPlayerAuthenticated())
		{
			OnPlayerAuthenticated();
			return;
		}
		AddAuthEvents();
		SocialManager.Instance.AuthenticatePlayer(true);
	}

	private void OnPlayerAuthenticated()
	{
		string @string = PlayerPrefs.GetString("SocialLogin", null);
		string text = SocialManager.Instance.PlayerIdentifierHash();
		if (!string.IsNullOrEmpty(@string) && @string != text)
		{
			PlayerInfoScript.ResetPlayerName();
		}
		PlayerPrefs.SetString("SocialLogin", text);
		SetRetrySocialLoginNextTime();
		if ((bool)DebugFlagsScript.GetInstance() && DebugFlagsScript.GetInstance().resetAchievements)
		{
			TFUtils.DebugLog("resetAchievements is true, attempting to reset...");
			SocialManager.Instance.ResetAllAchievements();
		}
		ClearAuthEvents();
		StartGameLoginFlow();
	}

	private void OnPlayerFailedToAuthenticate(string error)
	{
		ConfirmPopupController.ClickCallback clickCallback = delegate(bool yes)
		{
			if (yes)
			{
				SocialManager.Instance.AuthenticatePlayer(false);
			}
			else
			{
				ClearAuthEvents();
				StartGameLoginFlow();
			}
		};
		if (SocialManager.Instance.IsRetryAuth(error))
		{
			StartCoroutine(CoroutineShowPopup("!!SOCIAL_SIGN_IN_FAILED_RETRY", clickCallback));
		}
		else
		{
			clickCallback(false);
		}
	}

	private void AddAuthEvents()
	{
		SocialManager.Instance.playerAuthenticated += OnPlayerAuthenticated;
		SocialManager.Instance.playerFailedToAuthenticate += OnPlayerFailedToAuthenticate;
	}

	private void ClearAuthEvents()
	{
		SocialManager.Instance.playerAuthenticated -= OnPlayerAuthenticated;
		SocialManager.Instance.playerFailedToAuthenticate -= OnPlayerFailedToAuthenticate;
	}

	private void OnEnable()
	{
		HowOldAreYou.AgeGateDone += OnAgeGateDone;
	}

	private void OnDisable()
	{
		HowOldAreYou.AgeGateDone -= OnAgeGateDone;
	}

	private void OnAgeGateDone(int playerAge)
	{
		PlayerPrefs.SetInt("PlayerAge", playerAge);
		if (PlayerInfoScript.GetInstance().IsUnderage)
		{
			PlayerPrefs.DeleteKey("SocialLogin");
			StartGameLoginFlow();
		}
		else
		{
			Invoke("SocialLogin", 0.5f);
		}
	}

	private IEnumerator CoroutineShowPopup(string message, ConfirmPopupController.ClickCallback callback)
	{
		if (GeneralPopup == null || GeneralPopupShowTween == null)
		{
			if (callback != null)
			{
				yield return null;
				callback(false);
			}
			yield break;
		}
		bool hasResponse = false;
		bool confirmYes = false;
		ConfirmPopupController.ClickCallback interimCallback2 = null;
		interimCallback2 = delegate(bool yes)
		{
			ConfirmPopupController generalPopup2 = GeneralPopup;
			generalPopup2.OnSelect = (ConfirmPopupController.ClickCallback)Delegate.Remove(generalPopup2.OnSelect, interimCallback2);
			hasResponse = true;
			confirmYes = yes;
		};
		ConfirmPopupController generalPopup = GeneralPopup;
		generalPopup.OnSelect = (ConfirmPopupController.ClickCallback)Delegate.Combine(generalPopup.OnSelect, interimCallback2);
		GeneralPopup.Label = message;
		GeneralPopupShowTween.Play(true);
		while (!hasResponse)
		{
			yield return null;
		}
		yield return new WaitForSeconds(0.5f);
		if (callback != null)
		{
			callback(confirmYes);
		}
	}

	private void StartGameLoginFlow()
	{
		PlayerInfoScript.GetInstance().Login();
		LoadNextLevel();
	}

	public static void SetRetrySocialLoginNextTime()
	{
		if (!PlayerInfoScript.GetInstance().IsUnderage)
		{
			PlayerPrefs.SetString("RetrySocialLogin", "true");
		}
	}

	private static void OnSocialLogout()
	{
		PlayerPrefs.DeleteKey("RetrySocialLogin");
	}

	private void Awake()
	{
		if (!staticInited)
		{
			SocialManager.Instance.playerLoggedOut += OnSocialLogout;
			staticInited = true;
		}
	}

	private void Start()
	{
		AuthStarted = true;
		if (busyIconController == null)
		{
			busyIconController = SLOTGame.GetInstance();
		}
		if (!PlayerPrefs.HasKey("PlayerAge") && SocialManager.Instance.IsAgeGateRequired())
		{
			if (AgeGateShowTween != null)
			{
				AgeGateShowTween.Play(true);
			}
		}
		else if (!PlayerPrefs.HasKey("PlayerAge"))
		{
			OnAgeGateDone(100);
		}
		else if (PlayerPrefs.HasKey("RetrySocialLogin"))
		{
			SocialLogin();
		}
		else
		{
			StartGameLoginFlow();
		}
	}

	private void LoadNextLevel()
	{
		busyIconController.ShowBusyIcon(true);
		SLOTGameSingleton<SLOTSceneManager>.GetInstance().LoadLevelAsync(NextSceneName, LoadLevelDoneCallback);
	}

	private void Update()
	{
		if (!isComplete && PlayerInfoScript.GetInstance().IsReady())
		{
			isComplete = true;
		}
	}

	private void LoadLevelDoneCallback()
	{
		busyIconController.ShowBusyIcon(false);
	}
}
