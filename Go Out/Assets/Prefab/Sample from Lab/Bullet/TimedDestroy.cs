using UnityEngine;
using System.Collections;

public class TimedDestroy : MonoBehaviour {

	[SerializeField]
	float _destroyDelay = 5;
	[SerializeField] private AudioSource audioSource;
	[SerializeField] private AudioClip[] HitSounds;
	void Start()
	{
		audioSource = GetComponent<AudioSource>();
		
	}
	void OnEnable()
	{
		StartCoroutine (DelayDestroy ());
	}

	private IEnumerator DelayDestroy()
	{
		if (audioSource != null)
		{
			
			int n = (int)Random.Range(0, HitSounds.Length-1);
			audioSource.clip = HitSounds[n];
			audioSource.PlayOneShot(audioSource.clip, 0.1f);
		}
		yield return new WaitForSeconds(_destroyDelay);
		Destroy (gameObject);
	}
}
