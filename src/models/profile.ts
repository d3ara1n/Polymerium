import * as url from "url";

interface WrappedProfile {
    key: string,
    isLiked: boolean,
    profile: Profile
}

interface Profile {
    name: string,
    author: string,
    summary: string,
    reference: string | null,
    thumbnail: string | null,
    metadata: Metadata,
    timeline: TimelinePoint[]
}

interface Metadata {
}

interface TimelinePoint {

}