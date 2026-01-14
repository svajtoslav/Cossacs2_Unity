class LocalizationSettings:public BaseClass{
public:
	int LanguageSetForBikVideo;
	SAVE(LocalizationSettings);
	REG_MEMBER(_int,LanguageSetForBikVideo);
	ENDSAVE;
};
extern LocalizationSettings LocSettings;