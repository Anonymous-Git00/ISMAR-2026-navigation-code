using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sigtrap.VrTunnellingPro {
	/// <summary>
	/// Mobile-friendly tunnelling effect for legacy pipeline.<br />
	/// This script does not use post-processing. Limited to color, skybox and simple mask modes.
	/// </summary>
	public class TunnellingMobile : TunnellingMobileBase {
		/// <summary>
		/// Singleton instance.<br />
		/// Refers to a <see cref="TunnellingMobile"/> effect.<br />
		/// Will not refer to a <see cref="Tunnelling"/> or <see cref="TunnellingOpaque"/> effect.
		/// </summary>
		public static new TunnellingMobile instance { get; private set; }

        protected override void Awake(){
            base.Awake();

			if (instance != null){
				Debug.LogWarning("More than one VrTunnellingPro instance detected - tunnelling will work properly but singleton instance may not be the one you expect.");
			}
			instance = this;
        }
        
		void OnPreRender(){
			UpdateEyeMatrices();
			ApplyEyeMatrices(_irisMatOuter);
			ApplyEyeMatrices(_irisMatInner);
		}
	}
}